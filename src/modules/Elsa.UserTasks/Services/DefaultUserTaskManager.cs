using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.Common;
using Elsa.Workflows;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskManager(
    IUserTaskRepository repository,
    IUserTaskAccessPolicy accessPolicy,
    IEnumerable<IUserTaskFormProvider> formProviders,
    IUserTaskWorkflowResumer workflowResumer,
    IUserTaskNotificationSink notificationSink,
    IIdentityGenerator identityGenerator,
    ISystemClock clock,
    Microsoft.Extensions.Options.IOptions<UserTasksOptions> options,
    IUserTaskParticipantDirectory? participantDirectory = null) : IUserTaskManager
{
    private readonly IReadOnlyDictionary<string, IUserTaskFormProvider> _formProviders = formProviders
        .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    private readonly UserTasksOptions _options = options.Value;

    public async Task<UserTaskProjectionResult> ProjectAsync(UserTaskMaterialization materialization, CancellationToken cancellationToken = default)
    {
        var definition = materialization.Definition.Normalize();
        if (PayloadBytes(definition.TaskData) > _options.MaximumPayloadBytes || Encoding.UTF8.GetByteCount(definition.Instructions ?? "") > _options.MaximumPayloadBytes)
            throw new ArgumentException("User Task protected payload exceeds the configured limit.");
        var existing = await repository.FindByMaterializationKeyAsync(materialization.TenantId, MaterializationKey(materialization), cancellationToken);
        if (existing != null)
            return new UserTaskProjectionResult(existing, false);

        ResolvedUserTaskForm? pinnedForm = null;
        var healthSeverity = (UserTaskHealthSeverity?)null;
        string? healthCode = null;
        string? healthMessage = null;
        var snapshotMembers = materialization.SnapshotMembers.ToList();
        var snapshotGroups = materialization.SnapshotGroups.ToList();
        if (definition.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot)
        {
            snapshotMembers.AddRange(definition.CandidateUsers);
            snapshotGroups.AddRange(definition.CandidateGroups);
            if (participantDirectory != null)
            {
                foreach (var group in definition.CandidateGroups)
                {
                    try
                    {
                        snapshotMembers.AddRange(await participantDirectory.EnumerateGroupMembersAsync(group, cancellationToken));
                    }
                    catch (Exception)
                    {
                        healthSeverity = UserTaskHealthSeverity.Blocking;
                        healthCode = "snapshot-resolution-failed";
                        healthMessage = "A candidate group could not be resolved for this task.";
                    }
                }
            }
            else if (definition.CandidateGroups.Count > 0)
            {
                healthSeverity = UserTaskHealthSeverity.Blocking;
                healthCode = "snapshot-directory-missing";
                healthMessage = "A participant directory is required to snapshot candidate groups.";
            }
        }
        if (definition.FormReference != null)
        {
            if (!_formProviders.TryGetValue(definition.FormReference.ProviderName, out var provider))
            {
                healthSeverity = UserTaskHealthSeverity.Blocking;
                healthCode = "form-provider-missing";
                healthMessage = "The configured form provider is not available.";
            }
            else
            {
                pinnedForm = await provider.ResolveAsync(definition.FormReference, cancellationToken);
                if (pinnedForm == null)
                {
                    healthSeverity = UserTaskHealthSeverity.Blocking;
                    healthCode = "form-resolution-failed";
                    healthMessage = "The configured form could not be resolved.";
                }
            }
        }

        var task = new UserTask
        {
            Id = materialization.TaskId ?? identityGenerator.GenerateId(),
            TenantId = materialization.TenantId,
            WorkflowDefinitionId = materialization.WorkflowDefinitionId,
            WorkflowDefinitionName = materialization.WorkflowDefinitionName,
            WorkflowDefinitionVersion = materialization.WorkflowDefinitionVersion,
            WorkflowInstanceId = materialization.WorkflowInstanceId,
            WorkflowInstanceReference = materialization.WorkflowInstanceReference,
            ActivityInstanceId = materialization.ActivityInstanceId,
            BookmarkId = materialization.BookmarkId,
            MaterializationKey = MaterializationKey(materialization),
            Title = definition.Title,
            Summary = definition.Summary,
            Reference = definition.Reference,
            Tags = new HashSet<string>(definition.Tags, StringComparer.OrdinalIgnoreCase),
            TaskType = definition.TaskType,
            Requester = definition.Requester,
            Assignee = definition.Assignee,
            CandidateUsers = [..definition.CandidateUsers],
            CandidateGroups = [..definition.CandidateGroups],
            SnapshotMembers = [..snapshotMembers.Distinct()],
            SnapshotGroups = [..snapshotGroups.Distinct()],
            ExcludedUsers = [..definition.ExcludedUsers],
            MembershipResolutionMode = definition.MembershipResolutionMode,
            AllowManagerExclusionOverride = definition.AllowManagerExclusionOverride,
            Priority = definition.Priority,
            DueAt = definition.DueAt,
            Instructions = definition.Instructions,
            TaskData = definition.TaskData,
            RequestedForm = definition.FormReference,
            PinnedForm = pinnedForm,
            Actions = [..definition.Actions],
            InvitationDefinitions = [..definition.Invitations],
            EnableTimeoutOutcome = definition.EnableTimeoutOutcome,
            EnableCancellationOutcome = definition.EnableCancellationOutcome,
            Status = definition.Assignee != null ? UserTaskStatus.Assigned : HasCandidates(definition, materialization) ? UserTaskStatus.Available : UserTaskStatus.Unassigned,
            HealthSeverity = healthSeverity,
            HealthCode = healthCode,
            HealthMessage = healthMessage,
            CreatedAt = materialization.CreatedAt,
            UpdatedAt = materialization.CreatedAt
        };
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), task.TenantId, task.Id, 1, "Created", task.CreatedAt));
        await repository.AddProjectionAsync(task, cancellationToken);
        var projected = await repository.FindByMaterializationKeyAsync(task.TenantId, task.MaterializationKey, cancellationToken) ?? task;
        await notificationSink.PublishAsync(new UserTaskCreated(projected.TenantId, projected.Id, projected.Status, projected.Revision), cancellationToken);
        return new UserTaskProjectionResult(projected, true);
    }

    public async Task<UserTaskQueryResultDto?> QueryAsync(UserTaskQuery query, UserTaskQueryScopeKind scopeKind, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        // A null scope is a denial, not an absent filter. Falling through with the caller's raw query would
        // hand an unauthorized actor a tenant-wide page.
        var scope = await accessPolicy.CreateScopeAsync(actor, scopeKind, cancellationToken);
        if (scope == null)
            return null;

        var scopedQuery = query with { TenantId = actor.Subject.TenantId, Scope = scope };
        var result = await repository.QueryAsync(scopedQuery, cancellationToken);
        var summaries = new List<UserTaskSummary>();
        foreach (var item in result.Items)
            summaries.Add(await UserTaskModelMapper.ToSummaryAsync(item, actor, accessPolicy, cancellationToken));
        return new UserTaskQueryResultDto(summaries, result.NextCursor, result.TotalCount);
    }

    public async Task<UserTaskDetail?> GetAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadSummary, cancellationToken))
            return null;
        return await UserTaskModelMapper.ToDetailAsync(task, actor, accessPolicy, cancellationToken);
    }

    public async Task<UserTaskCapabilities?> GetCapabilitiesAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        return task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadSummary, cancellationToken)
            ? null
            : await UserTaskModelMapper.ToCapabilitiesAsync(task, actor, accessPolicy, cancellationToken);
    }

    public async Task<UserTaskEventsResult?> GetEventsAsync(string tenantId, string taskId, string? cursor, int limit, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadSummary, cancellationToken))
            return null;

        // Audit history is protected disclosure: a candidate who has never acted on the task sees the safe
        // summary but not who claimed, released, or completed it. Guests never see history at all.
        var canReadProtected = await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadProtected, cancellationToken);
        if (actor.IsGuest || !canReadProtected)
            return new UserTaskEventsResult([], null);

        var ordered = task.Events.OrderBy(x => x.OccurredAt).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var index = ordered.FindIndex(x => string.Equals(x.Id, DecodeEventCursor(cursor), StringComparison.Ordinal));
            startIndex = index < 0 ? ordered.Count : index + 1;
        }

        var pageSize = Math.Clamp(limit <= 0 ? 50 : limit, 1, 200);
        var page = ordered.Skip(startIndex).Take(pageSize).ToArray();
        var next = startIndex + page.Length < ordered.Count && page.Length > 0 ? EncodeEventCursor(page[^1].Id) : null;
        return new UserTaskEventsResult(page.Select(UserTaskModelMapper.ToEventSummary).ToArray(), next);
    }

    public async Task<JsonElement?> RevealFieldAsync(string tenantId, string taskId, string fieldKey, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null || !await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadProtected, cancellationToken))
            return null;

        // Only a field the form provider explicitly marked revealable can be disclosed, and only one at a
        // time. Unmarked or unknown keys are indistinguishable from an unauthorized task.
        var descriptor = task.PinnedForm?.Fields.FirstOrDefault(x => string.Equals(x.Key, fieldKey, StringComparison.Ordinal));
        if (descriptor is not { Masked: true, CanReveal: true })
            return null;

        var value = UserTaskModelMapper.ReadFieldValue(task.TaskData, fieldKey);
        if (value == null)
            return null;

        // Appended without bumping the revision: a reveal is a read, and consuming the concurrency token
        // here would make the caller's next command conflict for no reason.
        await repository.AppendEventAsync(tenantId, taskId, new(identityGenerator.GenerateId(), tenantId, taskId, task.Revision,
            "FieldRevealed", clock.UtcNow, actor.Subject, Metadata: new Dictionary<string, object?> { ["fieldKey"] = fieldKey }), cancellationToken);
        return value;
    }

    private static string EncodeEventCursor(string eventId) => Convert.ToBase64String(Encoding.UTF8.GetBytes(eventId));

    private static string? DecodeEventCursor(string cursor)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public Task<UserTaskOperationResult> ClaimAsync(string tenantId, string taskId, UserTaskMutationRequest request, UserTaskActor actor, CancellationToken cancellationToken = default) =>
        MutateAsync(tenantId, taskId, request.ExpectedRevision, request.OperationId, UserTaskOperationKind.Claim, actor, UserTaskAccessOperation.Claim,
            task =>
            {
                if (task.Status is not (UserTaskStatus.Available or UserTaskStatus.Unassigned))
                    return "not-claimable";
                task.Assignee = actor.Subject;
                task.AssignedAt = clock.UtcNow;
                task.Status = UserTaskStatus.Assigned;
                return null;
            }, cancellationToken);

    public Task<UserTaskOperationResult> ReleaseAsync(string tenantId, string taskId, UserTaskMutationRequest request, UserTaskActor actor, CancellationToken cancellationToken = default) =>
        MutateAsync(tenantId, taskId, request.ExpectedRevision, request.OperationId, UserTaskOperationKind.Release, actor, UserTaskAccessOperation.Release,
            task =>
            {
                task.Assignee = null;
                task.AssignedAt = null;
                task.Status = HasCandidates(task) ? UserTaskStatus.Available : UserTaskStatus.Unassigned;
                return null;
            }, cancellationToken);

    public Task<UserTaskOperationResult> AssignAsync(string tenantId, string taskId, UserTaskAssignRequest request, UserTaskActor actor, CancellationToken cancellationToken = default) =>
        MutateAsync(tenantId, taskId, request.ExpectedRevision, request.OperationId, UserTaskOperationKind.Assign, actor, UserTaskAccessOperation.Assign,
            task =>
            {
                if (!string.Equals(request.Assignee.TenantId, tenantId, StringComparison.Ordinal))
                    return "cross-tenant-assignee";
                var excluded = task.ExcludedUsers.Any(x => x.Matches(request.Assignee));
                if (excluded && (!task.AllowManagerExclusionOverride || string.IsNullOrWhiteSpace(request.Reason)))
                    return "excluded-assignee";
                if (task.IsTerminal)
                    return "terminal";
                task.Assignee = request.Assignee;
                task.AssignedAt = clock.UtcNow;
                task.Status = UserTaskStatus.Assigned;
                return null;
            }, cancellationToken, request.Reason, request.Assignee);

    public Task<UserTaskOperationResult> UpdateSchedulingAsync(string tenantId, string taskId, UserTaskSchedulingUpdate request, UserTaskActor actor, CancellationToken cancellationToken = default) =>
        MutateAsync(tenantId, taskId, request.ExpectedRevision, request.OperationId, UserTaskOperationKind.ScheduleUpdate, actor, UserTaskAccessOperation.UpdateScheduling,
            task =>
            {
                if (task.IsTerminal)
                    return "terminal";
                if (request.Priority is < 0 or > 100)
                    return "invalid-priority";
                if (request.Priority.HasValue)
                    task.Priority = request.Priority.Value;
                if (request.DueAt.HasValue)
                    task.DueAt = request.DueAt.Value;
                return null;
            }, cancellationToken, requestValue: new { request.Priority, request.DueAt });

    public async Task<UserTaskOperationResult> CompleteAsync(string tenantId, string taskId, UserTaskCompletionRequest request, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null)
            return Failure(taskId, tenantId, request.OperationId, UserTaskOperationKind.Complete, "not-found");
        if (!await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.Complete, cancellationToken))
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "forbidden");
        // A guest link is issued for a specific outcome set. Selecting any other configured action — even a
        // valid one — is an authorization failure, not a validation failure.
        if (actor.IsGuest && !actor.GuestAllowedActions.Contains(request.ActionKey))
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "forbidden");

        if (PayloadBytes(request.Data) > _options.MaximumPayloadBytes)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "payload-too-large");
        var action = task.Actions.FirstOrDefault(x => string.Equals(x.Key, request.ActionKey, StringComparison.Ordinal));
        var isTimeout = string.Equals(request.ActionKey, "Timeout", StringComparison.Ordinal);
        var isCancelled = string.Equals(request.ActionKey, "Cancelled", StringComparison.Ordinal);
        var existingOperation = FindOperation(task, request.OperationId);
        if (existingOperation != null && (isTimeout || isCancelled || action == null))
        {
            var rawHash = Hash(UserTaskOperationKind.Complete, new { request.ExpectedRevision, request.ActionKey, request.Data });
            return existingOperation.RequestHash == rawHash
                ? new UserTaskOperationResult(task, existingOperation, existingOperation.Status != UserTaskOperationStatus.Failed)
                : Failure(task, request.OperationId, UserTaskOperationKind.Complete, "idempotency-conflict");
        }
        if (isTimeout || isCancelled)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "reserved-action");
        if (action == null)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "invalid-action");
        if (task.RequestedForm == null && request.Data is { } data && data.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "form-required");
        if (task.RequestedForm != null && task.PinnedForm == null)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "form-resolution-failed");
        if (task.PinnedForm != null)
        {
            if (!_formProviders.TryGetValue(task.PinnedForm.Requested.ProviderName, out var provider))
                return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "form-provider-missing");
            var normalized = await provider.ValidateAndNormalizeAsync(task.PinnedForm, request.ActionKey, request.Data ?? JsonDocument.Parse("{}").RootElement, cancellationToken);
            if (!normalized.Succeeded)
                return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "form-invalid");
            request = request with { Data = normalized.NormalizedData };
            if (PayloadBytes(request.Data) > _options.MaximumPayloadBytes)
                return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "payload-too-large");
        }

        // Hash the canonical form data that will actually be persisted. This makes retries compare the
        // same request after provider normalization instead of comparing a raw pre-normalization shape.
        var requestHash = Hash(UserTaskOperationKind.Complete, new { request.ExpectedRevision, request.ActionKey, request.Data });
        var existing = FindOperation(task, request.OperationId);
        if (existing != null)
            return existing.RequestHash == requestHash ? new UserTaskOperationResult(task, existing, existing.Status != UserTaskOperationStatus.Failed) : Failure(task, request.OperationId, UserTaskOperationKind.Complete, "idempotency-conflict");
        if (task.Revision != request.ExpectedRevision)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "revision-conflict");
        if (task.IsTerminal)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "terminal");
        if (task.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling)
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "transition-in-progress");

        var operation = new UserTaskOperation(identityGenerator.GenerateId(), tenantId, taskId, request.OperationId, UserTaskOperationKind.Complete,
            request.ExpectedRevision, requestHash, UserTaskOperationStatus.Accepted,
            clock.UtcNow, clock.UtcNow, request.ActionKey, request.Data);
        task.Status = UserTaskStatus.Completing;
        task.CompletionActionKey = request.ActionKey;
        task.CompletionData = request.Data;
        task.CompletedBy = actor.Subject;
        task.Operations.Add(operation);
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, task.Revision + 1, "CompletionRequested", clock.UtcNow, actor.Subject, request.OperationId));
        try
        {
            await repository.SaveAsync(task, request.ExpectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Failure(task, request.OperationId, UserTaskOperationKind.Complete, "revision-conflict");
        }
        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notificationSink.PublishAsync(new UserTaskCompletionAccepted(tenantId, taskId, committed.Status, committed.Revision, request.OperationId), cancellationToken);
        await notificationSink.PublishAsync(new UserTaskChanged(tenantId, taskId, committed.Status, committed.Revision, ["completionRequested"]), cancellationToken);
        await workflowResumer.ResumeAsync(committed, new UserTaskStimulus(tenantId, taskId, request.OperationId, request.ActionKey, request.Data, actor.Subject, clock.UtcNow, committed.BookmarkId), cancellationToken);
        return new UserTaskOperationResult(committed, operation, true);
    }

    /// <summary>
    /// Reserves the timeout outcome for the due-date worker. This deliberately does not flow through
    /// <see cref="CompleteAsync"/>: reserved outcomes are system transitions and must not be callable
    /// as worker-selected actions.
    /// </summary>
    public async Task<UserTaskOperationResult> TimeoutAsync(string tenantId, string taskId, int expectedRevision, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null)
            return Failure(taskId, tenantId, $"timeout:{taskId}:{expectedRevision}", UserTaskOperationKind.Timeout, "not-found");

        var operationId = $"timeout:{task.Id}:{expectedRevision}";
        var hash = Hash(UserTaskOperationKind.Timeout, new { expectedRevision, dueAt = task.DueAt });
        var existing = FindOperation(task, operationId);
        if (existing != null)
        {
            if (existing.RequestHash != hash)
                return Failure(task, operationId, UserTaskOperationKind.Timeout, "idempotency-conflict");
            if (existing.Status != UserTaskOperationStatus.Accepted)
                return new UserTaskOperationResult(task, existing, existing.Status == UserTaskOperationStatus.Completed);

            // A previous delivery attempt may have failed after the accepted operation was committed.
            // Retrying the same bookmark stimulus is safe and lets reconciliation repair that gap.
            await workflowResumer.ResumeAsync(task, new UserTaskStimulus(tenantId, taskId, operationId, "Timeout", null, null, existing.CreatedAt, task.BookmarkId), cancellationToken);
            return new UserTaskOperationResult(task, existing, true);
        }

        if (task.IsTerminal)
            return Failure(task, operationId, UserTaskOperationKind.Timeout, "terminal");
        if (task.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling)
            return Failure(task, operationId, UserTaskOperationKind.Timeout, "transition-in-progress");
        if (!task.EnableTimeoutOutcome || task.DueAt is not { } dueAt || dueAt > now)
            return Failure(task, operationId, UserTaskOperationKind.Timeout, "timeout-not-due");
        if (task.Revision != expectedRevision)
            return Failure(task, operationId, UserTaskOperationKind.Timeout, "revision-conflict");

        var operation = new UserTaskOperation(
            identityGenerator.GenerateId(), tenantId, taskId, operationId, UserTaskOperationKind.Timeout,
            expectedRevision, hash, UserTaskOperationStatus.Accepted, clock.UtcNow, clock.UtcNow,
            ActionKey: "Timeout");
        task.IsOverdue = true;
        task.Status = UserTaskStatus.TimingOut;
        task.CompletionActionKey = "Timeout";
        task.Operations.Add(operation);
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, task.Revision + 1,
            "TimeoutRequested", clock.UtcNow, OperationId: operationId));
        try
        {
            await repository.SaveAsync(task, expectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Failure(task, operationId, UserTaskOperationKind.Timeout, "revision-conflict");
        }

        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notificationSink.PublishAsync(new UserTaskChanged(tenantId, taskId, committed.Status, committed.Revision, ["timeoutRequested"]), cancellationToken);
        await notificationSink.PublishAsync(new UserTaskOverdue(tenantId, taskId, committed.Status, committed.Revision), cancellationToken);
        await workflowResumer.ResumeAsync(committed, new UserTaskStimulus(tenantId, taskId, operationId, "Timeout", null, null, now, committed.BookmarkId), cancellationToken);
        var committedOperation = FindOperation(committed, operationId) ?? operation;
        return new UserTaskOperationResult(committed, committedOperation, true);
    }

    public async Task<UserTaskOperationResult> CancelAsync(string tenantId, string taskId, UserTaskCancelRequest request, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null)
            return Failure(taskId, tenantId, request.OperationId, UserTaskOperationKind.Cancel, "not-found");
        if (!await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.Cancel, cancellationToken))
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "forbidden");
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "reason-required");
        if (!task.EnableCancellationOutcome)
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "cancellation-disabled");
        var hash = Hash(UserTaskOperationKind.Cancel, new { request.ExpectedRevision, request.Reason });
        var existing = FindOperation(task, request.OperationId);
        if (existing != null)
            return existing.RequestHash == hash ? new UserTaskOperationResult(task, existing, existing.Status != UserTaskOperationStatus.Failed) : Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "idempotency-conflict");
        if (task.IsTerminal)
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "terminal");
        if (task.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling)
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "transition-in-progress");
        if (task.Revision != request.ExpectedRevision)
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "revision-conflict");
        var operation = new UserTaskOperation(identityGenerator.GenerateId(), tenantId, taskId, request.OperationId, UserTaskOperationKind.Cancel,
            request.ExpectedRevision, hash, UserTaskOperationStatus.Accepted, clock.UtcNow, clock.UtcNow, ActionKey: "Cancelled", Data: null, ErrorCode: null);
        task.Status = UserTaskStatus.Cancelling;
        task.CompletionActionKey = "Cancelled";
        task.CompletedBy = actor.Subject;
        task.Operations.Add(operation);
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, task.Revision + 1, "CancellationRequested", clock.UtcNow, actor.Subject, request.OperationId, request.Reason));
        try
        {
            await repository.SaveAsync(task, request.ExpectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Failure(task, request.OperationId, UserTaskOperationKind.Cancel, "revision-conflict");
        }
        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notificationSink.PublishAsync(new UserTaskChanged(tenantId, taskId, committed.Status, committed.Revision, ["cancellationRequested"]), cancellationToken);
        await workflowResumer.ResumeAsync(committed, new UserTaskStimulus(tenantId, taskId, request.OperationId, "Cancelled", null, actor.Subject, clock.UtcNow, committed.BookmarkId), cancellationToken);
        return new UserTaskOperationResult(committed, operation, true);
    }

    public async Task<UserTaskOperationResult> RetryResolutionAsync(string tenantId, string taskId, UserTaskMutationRequest request, UserTaskActor actor, CancellationToken cancellationToken = default)
    {
        var operationId = request.OperationId ?? identityGenerator.GenerateId();
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null)
            return Failure(taskId, tenantId, operationId, UserTaskOperationKind.RetryResolution, "not-found");
        if (!await accessPolicy.AuthorizeAsync(task, actor, UserTaskAccessOperation.RetryResolution, cancellationToken))
            return Failure(task, operationId, UserTaskOperationKind.RetryResolution, "forbidden");
        var hash = Hash(UserTaskOperationKind.RetryResolution, new { request.ExpectedRevision });
        var existing = FindOperation(task, operationId);
        if (existing != null)
            return existing.RequestHash == hash ? new UserTaskOperationResult(task, existing, existing.Status != UserTaskOperationStatus.Failed) : Failure(task, operationId, UserTaskOperationKind.RetryResolution, "idempotency-conflict");
        if (task.Revision != request.ExpectedRevision)
            return Failure(task, operationId, UserTaskOperationKind.RetryResolution, "revision-conflict");

        if (task.RequestedForm != null)
        {
            if (_formProviders.TryGetValue(task.RequestedForm.ProviderName, out var provider))
                task.PinnedForm = await provider.ResolveAsync(task.RequestedForm, cancellationToken);
            else
                task.PinnedForm = null;
        }
        UserTaskHealthSeverity? healthSeverity = task.RequestedForm != null && task.PinnedForm == null ? UserTaskHealthSeverity.Blocking : null;
        var healthCode = healthSeverity == UserTaskHealthSeverity.Blocking ? "form-resolution-failed" : null;
        var healthMessage = healthSeverity == UserTaskHealthSeverity.Blocking ? "The configured form could not be resolved." : null;
        if (task.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot && task.CandidateGroups.Count > 0)
        {
            var snapshotMembers = task.CandidateUsers.ToList();
            var snapshotGroups = task.CandidateGroups.ToList();
            if (participantDirectory == null)
            {
                healthSeverity = UserTaskHealthSeverity.Blocking;
                healthCode = "snapshot-directory-missing";
                healthMessage = "A participant directory is required to snapshot candidate groups.";
            }
            else
            {
                foreach (var group in task.CandidateGroups)
                {
                    try
                    {
                        snapshotMembers.AddRange(await participantDirectory.EnumerateGroupMembersAsync(group, cancellationToken));
                    }
                    catch
                    {
                        healthSeverity = UserTaskHealthSeverity.Blocking;
                        healthCode = "snapshot-resolution-failed";
                        healthMessage = "A candidate group could not be resolved for this task.";
                    }
                }
            }
            task.SnapshotMembers = [..snapshotMembers.Distinct()];
            task.SnapshotGroups = [..snapshotGroups.Distinct()];
        }
        task.HealthSeverity = healthSeverity;
        task.HealthCode = healthCode;
        task.HealthMessage = healthMessage;
        var operation = new UserTaskOperation(identityGenerator.GenerateId(), tenantId, taskId, operationId, UserTaskOperationKind.RetryResolution,
            request.ExpectedRevision, hash, UserTaskOperationStatus.Completed, clock.UtcNow, clock.UtcNow);
        task.Operations.Add(operation);
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, task.Revision + 1, "ResolutionRetried", clock.UtcNow, actor.Subject, operationId));
        try
        {
            await repository.SaveAsync(task, request.ExpectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Failure(task, operationId, UserTaskOperationKind.RetryResolution, "revision-conflict");
        }
        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notificationSink.PublishAsync(new UserTaskHealthChanged(tenantId, taskId, committed.Status, committed.Revision), cancellationToken);
        return new UserTaskOperationResult(committed, operation, true);
    }

    private async Task<UserTaskOperationResult> MutateAsync(string tenantId, string taskId, int expectedRevision, string? requestedOperationId,
        UserTaskOperationKind kind, UserTaskActor actor, UserTaskAccessOperation access, Func<UserTask, string?> mutation, CancellationToken cancellationToken, string? reason = null, object? requestValue = null)
    {
        var operationId = requestedOperationId ?? identityGenerator.GenerateId();
        var task = await repository.GetAsync(tenantId, taskId, cancellationToken);
        if (task == null)
            return Failure(taskId, tenantId, operationId, kind, "not-found");
        if (!await accessPolicy.AuthorizeAsync(task, actor, access, cancellationToken))
            return Failure(task, operationId, kind, "forbidden");
        var hash = Hash(kind, new { expectedRevision, reason, requestValue, actor.Subject });
        var existing = FindOperation(task, operationId);
        if (existing != null)
            return existing.RequestHash == hash ? new UserTaskOperationResult(task, existing, existing.Status == UserTaskOperationStatus.Completed) : Failure(task, operationId, kind, "idempotency-conflict");
        if (task.Revision != expectedRevision)
            return Failure(task, operationId, kind, "revision-conflict");
        if (task.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling)
            return Failure(task, operationId, kind, "transition-in-progress");
        var reasonCode = mutation(task);
        if (reasonCode != null)
            return Failure(task, operationId, kind, reasonCode);
        var operation = new UserTaskOperation(identityGenerator.GenerateId(), tenantId, taskId, operationId, kind, expectedRevision,
            hash, UserTaskOperationStatus.Completed, clock.UtcNow, clock.UtcNow);
        task.Operations.Add(operation);
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, taskId, expectedRevision + 1, kind.ToString(), clock.UtcNow, actor.Subject, operationId, reason));
        try
        {
            await repository.SaveAsync(task, expectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Failure(task, operationId, kind, "revision-conflict");
        }
        var committed = await repository.GetAsync(tenantId, taskId, cancellationToken) ?? task;
        await notificationSink.PublishAsync(new UserTaskChanged(tenantId, taskId, committed.Status, committed.Revision, [kind.ToString()]), cancellationToken);
        return new UserTaskOperationResult(committed, operation, true);
    }

    private static UserTaskOperation? FindOperation(UserTask task, string operationId) => task.Operations.FirstOrDefault(x => x.OperationId == operationId);
    private static bool HasCandidates(UserTaskDefinitionSnapshot definition, UserTaskMaterialization materialization) => definition.Assignee != null || definition.CandidateUsers.Count > 0 || definition.CandidateGroups.Count > 0 || materialization.SnapshotMembers.Count > 0 || definition.Invitations.Count > 0;
    private static bool HasCandidates(UserTask task) => task.CandidateUsers.Count > 0 || task.CandidateGroups.Count > 0 || task.SnapshotMembers.Count > 0 || task.InvitationDefinitions.Count > 0;
    private static string MaterializationKey(UserTaskMaterialization materialization) => string.Join("/", materialization.TenantId, materialization.WorkflowInstanceId, materialization.ActivityInstanceId);
    private static string Hash(UserTaskOperationKind kind, object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kind + ":" + JsonSerializer.Serialize(value))));
    private static int PayloadBytes(JsonElement? value) => value is { } element ? JsonSerializer.SerializeToUtf8Bytes(element).Length : 0;

    private UserTaskOperationResult Failure(string taskId, string tenantId, string operationId, UserTaskOperationKind kind, string code) =>
        Failure(new UserTask { Id = taskId, TenantId = tenantId }, operationId, kind, code);

    private static UserTaskOperationResult Failure(UserTask task, string operationId, UserTaskOperationKind kind, string code)
    {
        var operation = new UserTaskOperation(Guid.NewGuid().ToString("N"), task.TenantId, task.Id, operationId, kind, task.Revision,
            "", UserTaskOperationStatus.Failed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ErrorCode: code);
        return new UserTaskOperationResult(task, operation, false, code);
    }
}
