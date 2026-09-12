using System.Text.Json;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Services;

/// <summary>
/// Projects the internal aggregate onto the wire contract. Every protected value passes through an explicit
/// policy decision here, so an endpoint can never widen disclosure by choosing a different response type.
/// </summary>
public static class UserTaskModelMapper
{
    private static readonly UserTaskAccessOperation[] ActionOperations =
    [
        UserTaskAccessOperation.Claim,
        UserTaskAccessOperation.Release,
        UserTaskAccessOperation.Assign,
        UserTaskAccessOperation.UpdateScheduling,
        UserTaskAccessOperation.Complete,
        UserTaskAccessOperation.Cancel,
        UserTaskAccessOperation.IssueInvitation,
        UserTaskAccessOperation.RetryResolution
    ];

    public static string ActionName(UserTaskAccessOperation operation) => operation switch
    {
        UserTaskAccessOperation.Claim => "claim",
        UserTaskAccessOperation.Release => "release",
        UserTaskAccessOperation.Assign => "assign",
        UserTaskAccessOperation.UpdateScheduling => "update-scheduling",
        UserTaskAccessOperation.Complete => "complete",
        UserTaskAccessOperation.Cancel => "cancel",
        UserTaskAccessOperation.IssueInvitation => "invite",
        UserTaskAccessOperation.RetryResolution => "retry-resolution",
        _ => operation.ToString().ToLowerInvariant()
    };

    public static async Task<UserTaskSummary> ToSummaryAsync(UserTask task, UserTaskActor actor, IUserTaskAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        var allowed = new List<string>();
        foreach (var operation in ActionOperations)
        {
            if (await policy.AuthorizeAsync(task, actor, operation, cancellationToken))
                allowed.Add(ActionName(operation));
        }

        // Blocking health is an operator signal. Surfacing it to an ordinary participant would leak that a
        // directory or form provider failed, so it is folded away unless the caller manages the tenant.
        var healthVisible = actor.IsManager && !actor.IsGuest;
        var workflowVisible = !actor.IsGuest;
        return new UserTaskSummary
        {
            Id = task.Id,
            Title = task.Title,
            Summary = task.Summary,
            Reference = task.Reference,
            Tags = task.Tags.ToArray(),
            TaskType = task.TaskType,
            Status = task.Status.ToString(),
            Priority = task.Priority,
            Assignee = actor.IsGuest ? null : UserTaskParticipantSummary.From(task.Assignee),
            CandidateSummary = actor.IsGuest ? null : DescribeCandidates(task),
            DueAt = task.DueAt,
            IsOverdue = task.IsOverdue,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            AssignedAt = actor.IsGuest ? null : task.AssignedAt,
            CompletedAt = task.CompletedAt,
            WorkflowDefinitionId = workflowVisible ? NullIfEmpty(task.WorkflowDefinitionId) : null,
            WorkflowDefinitionName = workflowVisible ? task.WorkflowDefinitionName : null,
            WorkflowDefinitionVersion = workflowVisible ? task.WorkflowDefinitionVersion : null,
            WorkflowInstanceId = workflowVisible ? NullIfEmpty(task.WorkflowInstanceId) : null,
            WorkflowInstanceReference = workflowVisible ? task.WorkflowInstanceReference : null,
            HealthSeverity = healthVisible ? task.HealthSeverity?.ToString() : null,
            HealthCode = healthVisible ? task.HealthCode : null,
            AllowedActions = allowed,
            Revision = task.Revision
        };
    }

    public static async Task<UserTaskDetail> ToDetailAsync(UserTask task, UserTaskActor actor, IUserTaskAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        var summary = await ToSummaryAsync(task, actor, policy, cancellationToken);
        var canReadProtected = await policy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadProtected, cancellationToken);
        var canViewHistory = !actor.IsGuest && (actor.IsManager || canReadProtected);
        var disclosure = new UserTaskDisclosure
        {
            CanViewProtected = canReadProtected,
            CanViewWorkflow = !actor.IsGuest,
            CanViewHistory = canViewHistory,
            GuestVisible = actor.IsGuest
        };

        // Guests may only complete the action keys their invitation was issued for, so the action list they
        // receive is intersected with that allowlist rather than showing the workflow's full action set.
        var actions = task.Actions
            .Where(action => !actor.IsGuest || actor.GuestAllowedActions.Contains(action.Key))
            .Select(action => new UserTaskFormAction(action.Key, action.Label))
            .ToArray();

        return new UserTaskDetail
        {
            Id = summary.Id,
            Title = summary.Title,
            Summary = summary.Summary,
            Reference = summary.Reference,
            Tags = summary.Tags,
            TaskType = summary.TaskType,
            Status = summary.Status,
            Priority = summary.Priority,
            Assignee = summary.Assignee,
            CandidateSummary = summary.CandidateSummary,
            DueAt = summary.DueAt,
            IsOverdue = summary.IsOverdue,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            AssignedAt = summary.AssignedAt,
            CompletedAt = summary.CompletedAt,
            WorkflowDefinitionId = summary.WorkflowDefinitionId,
            WorkflowDefinitionName = summary.WorkflowDefinitionName,
            WorkflowDefinitionVersion = summary.WorkflowDefinitionVersion,
            WorkflowInstanceId = summary.WorkflowInstanceId,
            WorkflowInstanceReference = summary.WorkflowInstanceReference,
            HealthSeverity = summary.HealthSeverity,
            HealthCode = summary.HealthCode,
            AllowedActions = summary.AllowedActions,
            Revision = summary.Revision,
            Instructions = canReadProtected ? task.Instructions : null,
            Data = canReadProtected ? task.TaskData : null,
            Disclosure = disclosure,
            Workflow = disclosure.CanViewWorkflow ? ToWorkflowContext(task) : null,
            Form = ToFormProjection(task, actions, canReadProtected),
            Actions = actions,
            Outcome = canReadProtected ? task.CompletionActionKey : null,
            Response = canReadProtected ? task.CompletionData : null,
            CompletedBy = canReadProtected && !actor.IsGuest ? UserTaskParticipantSummary.From(task.CompletedBy) : null
        };
    }

    public static async Task<UserTaskCapabilities> ToCapabilitiesAsync(UserTask task, UserTaskActor actor, IUserTaskAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        var summary = await ToSummaryAsync(task, actor, policy, cancellationToken);
        return new UserTaskCapabilities(task.Id, task.Revision, summary.AllowedActions,
            await policy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadProtected, cancellationToken),
            actor.IsManager && !actor.IsGuest);
    }

    public static UserTaskEventSummary ToEventSummary(UserTaskEvent @event) =>
        new(@event.Id, @event.EventType, @event.Reason, @event.OccurredAt, @event.Actor?.DisplayName);

    private static UserTaskWorkflowContext ToWorkflowContext(UserTask task) => new()
    {
        DefinitionId = NullIfEmpty(task.WorkflowDefinitionId),
        DefinitionName = task.WorkflowDefinitionName,
        DefinitionVersion = task.WorkflowDefinitionVersion,
        InstanceId = NullIfEmpty(task.WorkflowInstanceId),
        InstanceReference = task.WorkflowInstanceReference
    };

    private static UserTaskFormProjection? ToFormProjection(UserTask task, IReadOnlyCollection<UserTaskFormAction> actions, bool canReadProtected)
    {
        if (task.PinnedForm is not { } form)
            return null;

        var fields = form.Fields.Select(descriptor => new UserTaskFormField
        {
            Key = descriptor.Key,
            Label = descriptor.Label,
            Type = descriptor.Type,
            Required = descriptor.Required,
            Masked = descriptor.Masked,
            CanReveal = descriptor.Masked && descriptor.CanReveal && canReadProtected,
            // A masked value never rides along with the form. It is disclosed only through the explicit,
            // audited reveal command, so an accidental log or screenshot of the detail response is inert.
            Value = canReadProtected && !descriptor.Masked ? ReadFieldValue(task.TaskData, descriptor.Key) : null
        }).ToArray();

        return new UserTaskFormProjection
        {
            Provider = form.Requested.ProviderName,
            Key = form.Requested.Key,
            Version = form.PinnedVersion,
            Fields = fields,
            Actions = actions
        };
    }

    internal static JsonElement? ReadFieldValue(JsonElement? data, string key) =>
        data is { ValueKind: JsonValueKind.Object } element && element.TryGetProperty(key, out var value) ? value.Clone() : null;

    private static string? DescribeCandidates(UserTask task)
    {
        var users = task.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot
            ? task.SnapshotMembers.Count(x => x.Type == UserTaskParticipantType.User)
            : task.CandidateUsers.Count;
        var groups = task.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot
            ? task.SnapshotGroups.Count
            : task.CandidateGroups.Count;
        if (users == 0 && groups == 0)
            return null;

        // Counts only: disclosing which peers are eligible would let any candidate enumerate the others.
        var parts = new List<string>(2);
        if (users > 0)
            parts.Add(users == 1 ? "1 user" : $"{users} users");
        if (groups > 0)
            parts.Add(groups == 1 ? "1 group" : $"{groups} groups");
        return string.Join(", ", parts);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
