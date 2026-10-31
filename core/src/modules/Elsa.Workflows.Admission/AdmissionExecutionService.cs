using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Admission;

/// <summary>Observable protocol boundaries, useful for deterministic host fault testing; never conveys execution authority.</summary>
public enum AdmissionExecutionBoundary
{
    CreationClaimed, InstanceInsertedAndNotified, StartPrepared, StartAuthorized, BeforeRunnerEntry,
    AuthorityConsumed, RunnerReturned, TrailingWriteCompleted, OwnershipUnwound, CheckpointRecorded
}

/// <summary>Optional trusted host instrumentation. Implementations must not log payloads or credentials.</summary>
public interface IAdmissionExecutionObserver
{
    ValueTask ObserveAsync(AdmissionExecutionBoundary boundary, string admissionId, string instanceId, CancellationToken cancellationToken);
}

/// <summary>Bounded recovery metadata; excludes provider identity, configuration, inputs and payload digests.</summary>
public sealed record AdmissionRecoveryItem(string AdmissionId, long Revision, AdmissionState State, bool HasInstance, bool AuthorityOutstanding);
/// <summary>Exclusive ID cursor. Restart a scan to visit rows inserted behind an earlier cursor.</summary>
public sealed record AdmissionRecoveryPage(IReadOnlyList<AdmissionRecoveryItem> Items, string? AfterId);

/// <summary>
/// Transport-independent coordinator for one isolated in-process LocalWorkflowRuntime host.
/// Persisted states, readback and restart never mint an invocable permit.
/// </summary>
public sealed class AdmissionExecutionService
{
    private readonly IAdmissionStore _store;
    private readonly AdmissionAuthorityRegistry _authorities;
    private readonly IServiceProvider _services;
    private readonly IWorkflowDefinitionService _definitions;
    private readonly IWorkflowInstanceManager _instances;
    private readonly IWorkflowRunner _runner;
    private readonly IWorkflowStateSerializer _stateSerializer;
    private readonly IWorkflowStateExtractor _extractor;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly IBookmarkStore _bookmarks;
    private readonly ISystemClock _clock;
    private readonly ITenantAccessor _tenant;
    private readonly AdmissionHostConfiguration _host;

    internal AdmissionExecutionService(IAdmissionStore store, AdmissionAuthorityRegistry authorities, IServiceProvider services,
        IWorkflowDefinitionService definitions, IWorkflowInstanceManager instances, IWorkflowRunner runner,
        IWorkflowStateSerializer stateSerializer, IWorkflowStateExtractor extractor, IActivitySerializer activitySerializer,
        IPayloadSerializer payloadSerializer, IBookmarkStore bookmarks, ISystemClock clock, ITenantAccessor tenant, AdmissionHostConfiguration host)
    {
        _store = store; _authorities = authorities; _services = services; _definitions = definitions; _instances = instances;
        _runner = runner; _stateSerializer = stateSerializer; _extractor = extractor; _activitySerializer = activitySerializer;
        _payloadSerializer = payloadSerializer; _bookmarks = bookmarks; _clock = clock; _tenant = tenant; _host = host;
    }

    /// <summary>Only a definite store return establishes acknowledgement eligibility. No transport acknowledgement is sent here.</summary>
    public async Task<AdmissionResult> AdmitAsync(AdmissionEvent message, CancellationToken cancellationToken = default)
    {
        await _host.ValidateAsync(_services, cancellationToken);
        var subscription = await _store.FindSubscriptionAsync(message.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("The trusted admission subscription is missing.");
        ValidateScope(subscription.Configuration);
        _host.ValidateSubscription(subscription.Configuration);
        await LoadPinnedGraphAsync(subscription.Configuration, cancellationToken);
        return await _store.AdmitAsync(message, _clock.UtcNow, cancellationToken);
    }

    /// <summary>Materializes and starts eligible admitted work once. Known duplicate/noninitial states are not replayed.</summary>
    public async Task<RunWorkflowInstanceResponse?> ExecuteAsync(string admissionId, CancellationToken cancellationToken = default)
    {
        await _host.ValidateAsync(_services, cancellationToken);
        var record = await FindRequiredAsync(admissionId, cancellationToken);
        if (record.State is not (AdmissionState.Admitted or AdmissionState.Materialized))
        {
            return null;
        }
        var configuration = Configuration(record);
        ValidateScope(configuration);
        _host.ValidateSubscription(configuration);
        var graph = await LoadPinnedGraphAsync(configuration, cancellationToken);
        if (record.State == AdmissionState.Admitted)
        {
            var instanceId = Guid.NewGuid().ToString("N");
            // Register local quiescence ownership before the durable claim can become visible.
            // Resolution must not retire Creating while its original caller can still insert.
            using var owner = _authorities.AcquireOwner(instanceId);
            record = await _store.BeginCreationAsync(record.Id, record.Revision, instanceId, cancellationToken);
            if (record == null)
            {
                return null;
            }
            WorkflowState materializedState;
            try
            {
                await ObserveAsync(AdmissionExecutionBoundary.CreationClaimed, record, cancellationToken);
                var instance = _instances.CreateWorkflowInstance(graph.Workflow, new WorkflowInstanceOptions
                {
                    WorkflowInstanceId = instanceId,
                    Input = new Dictionary<string, object>
                    {
                        ["Event"] = record.Payload ?? throw new InvalidOperationException("The admitted payload is unavailable."),
                        ["ProviderEventId"] = record.ProviderEventId!,
                        ["ChannelId"] = configuration.ChannelId
                    }
                });
                // Real insert-only manager, including its saved-notification chain. A matching
                // row after an exception cannot prove these notifications completed.
                var expectedState = _stateSerializer.Serialize(instance.WorkflowState);
                await _instances.CreateAsync(instance, cancellationToken);
                await ObserveAsync(AdmissionExecutionBoundary.InstanceInsertedAndNotified, record, cancellationToken);
                var reloaded = await _instances.FindByIdAsync(instanceId, cancellationToken)
                    ?? throw new InvalidOperationException("The inserted admission instance is missing.");
                if (_stateSerializer.Serialize(reloaded.WorkflowState) != expectedState)
                {
                    throw new InvalidOperationException("The inserted admission state changed before materialization completed.");
                }
                record = await _store.CompleteCreationAsync(record.Id, record.Revision, AdmissionHash.Compute(expectedState), cancellationToken)
                    ?? throw new InvalidOperationException("Admission creation completion lost its revision.");
                materializedState = reloaded.WorkflowState;
            }
            catch
            {
                await MarkRecoveryAsync(admissionId, "creation-or-start-incomplete");
                throw;
            }
            return await RunOwnedAsync(record, configuration, graph, materializedState, null, null, false, cancellationToken);
        }
        using (var owner = _authorities.AcquireOwner(record.WorkflowInstanceId!))
        {
            // A caller can read Materialized before another local owner runs and unwinds.
            // Losing that race is a definite duplicate, not this caller's uncertain attempt.
            if (!await IsCurrentOwnedSnapshotAsync(record, cancellationToken))
            {
                return null;
            }
            WorkflowState materializedState;
            try
            {
                var instance = await _instances.FindByIdAsync(record.WorkflowInstanceId!, cancellationToken)
                    ?? throw new InvalidOperationException("The materialized admission instance is missing.");
                ValidateInitialState(instance.WorkflowState, record, configuration);
                materializedState = instance.WorkflowState;
            }
            catch
            {
                await MarkRecoveryAsync(admissionId, "materialized-state-incomplete");
                throw;
            }
            return await RunOwnedAsync(record, configuration, graph, materializedState, null, null, false, cancellationToken);
        }
    }

    internal async Task<RunWorkflowInstanceResponse> ResumeAsync(AdmissionRecord record, RunWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        await _host.ValidateAsync(_services, cancellationToken);
        if (record.State != AdmissionState.ExecutionObserved || string.IsNullOrEmpty(request.BookmarkId) || request.ActivityHandle != null ||
            request.Variables != null || request.Properties != null || request.TriggerActivityId != null ||
            request.SchedulingActivityExecutionId != null || request.SchedulingWorkflowInstanceId != null || request.SchedulingCallStackDepth != null)
        {
            throw new InvalidOperationException("Owned continuation requires an exact trusted bookmark lineage.");
        }
        using var owner = _authorities.AcquireOwner(record.WorkflowInstanceId!);
        if (!await IsCurrentOwnedSnapshotAsync(record, cancellationToken))
        {
            throw new InvalidOperationException("The admission continuation snapshot was superseded before ownership acquisition.");
        }
        var configuration = Configuration(record);
        ValidateScope(configuration);
        _host.ValidateSubscription(configuration);
        var instance = await _instances.FindByIdAsync(record.WorkflowInstanceId!, cancellationToken)
            ?? throw new InvalidOperationException("The admission checkpoint instance is missing.");
        var state = instance.WorkflowState;
        var checkpoint = await CheckpointFingerprintAsync(state, cancellationToken);
        if (checkpoint != record.CheckpointFingerprint || !JsonSerializer.Deserialize<string[]>(record.BookmarkIdsJson ?? "[]")!.Contains(request.BookmarkId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The admission checkpoint or bookmark lineage changed.");
        }
        var bookmark = state.Bookmarks.SingleOrDefault(x => x.Id == request.BookmarkId);
        if (bookmark?.ActivityInstanceId == null || !state.ActivityExecutionContexts.Any(x => x.Id == bookmark.ActivityInstanceId))
        {
            throw new InvalidOperationException("The admission bookmark has no trusted owning activity.");
        }
        var graph = await LoadPinnedGraphAsync(configuration, cancellationToken);
        return await RunOwnedAsync(record, configuration, graph, state, bookmark, request.Input, request.IncludeWorkflowOutput, cancellationToken);
    }

    /// <summary>Conservative restart classification; never repeats insertion, notifications, permit or effects.</summary>
    public async Task RecoverAsync(string admissionId, CancellationToken cancellationToken = default)
    {
        await _host.ValidateAsync(_services, cancellationToken);
        var record = await FindRequiredAsync(admissionId, cancellationToken);
        ValidateScope(Configuration(record));
        if (record.WorkflowInstanceId != null && _authorities.HasOwner(record.WorkflowInstanceId))
        {
            throw new InvalidOperationException("A delayed or active local owner is not quiescent.");
        }
        if (record.State is AdmissionState.Creating or AdmissionState.StartPreparing or AdmissionState.StartAuthorized)
        {
            await _store.RequireRecoveryAsync(record.Id, record.Revision, "restart-boundary-unknown", cancellationToken);
        }
    }

    public async Task<AdmissionRecoveryPage> ListRecoveryAsync(int limit, string? afterId = null, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000 || afterId is { Length: > 256 } || (afterId != null && string.IsNullOrWhiteSpace(afterId)))
        {
            throw new ArgumentException("Recovery discovery requires a bounded page and explicit nonempty cursor.");
        }
        await _host.ValidateAsync(_services, cancellationToken);
        if (_tenant.TenantId != _host.TenantId)
        {
            throw new InvalidOperationException("Recovery discovery cannot select a different host tenant.");
        }
        var records = await _store.FindRecoverableAsync(limit, afterId, cancellationToken);
        var items = records.Select(x => new AdmissionRecoveryItem(x.Id, x.Revision, x.State, x.WorkflowInstanceId != null, x.AuthorityOutstanding)).ToArray();
        return new(items, records.Count == limit ? records[^1].Id : null);
    }

    /// <summary>Audited operator resolution after established host-wide quiescence; never infers remote effect completion.</summary>
    public async Task<AdmissionRecord?> ResolveAsync(string admissionId, long revision, AdmissionTerminalDisposition disposition,
        string auditReference, bool operatorEstablishedAllOwnerQuiescence, bool operatorEstablishedNoUnknownEffects, CancellationToken cancellationToken = default)
    {
        await _host.ValidateAsync(_services, cancellationToken);
        var record = await FindRequiredAsync(admissionId, cancellationToken);
        ValidateScope(Configuration(record));
        if (!operatorEstablishedAllOwnerQuiescence || !operatorEstablishedNoUnknownEffects || string.IsNullOrWhiteSpace(auditReference) ||
            System.Text.Encoding.UTF8.GetByteCount(auditReference) > AdmissionLimits.AuthorityBytes ||
            (record.WorkflowInstanceId != null && _authorities.HasOwner(record.WorkflowInstanceId)))
        {
            throw new InvalidOperationException("Audited resolution requires all-owner quiescence and no unknown effects.");
        }
        if (record.WorkflowInstanceId != null)
        {
            var instance = await _instances.FindByIdAsync(record.WorkflowInstanceId, cancellationToken);
            if (instance is { WorkflowState.IsExecuting: true })
            {
                throw new InvalidOperationException("The workflow has not established execution quiescence.");
            }
        }
        return await _store.ResolveAsync(admissionId, revision, disposition, auditReference, operatorEstablishedAllOwnerQuiescence, operatorEstablishedNoUnknownEffects, _clock.UtcNow, cancellationToken);
    }

    private async Task<RunWorkflowInstanceResponse> RunOwnedAsync(AdmissionRecord record, AdmissionSubscriptionConfiguration configuration,
        WorkflowGraph graph, WorkflowState state, Bookmark? bookmark, IDictionary<string, object>? input, bool includeWorkflowOutput, CancellationToken cancellationToken)
    {
        WorkflowExecutionContext? context = null;
        WorkflowInstance persisted;
        try
        {
            // Typed input validation precedes callbacks. The host audits the fixed framework
            // registry/materialization/restore chain; arbitrary custom objects are unsupported.
            AdmissionValueFingerprint.Compute(input ?? new Dictionary<string, object>());
            context = await WorkflowExecutionContext.CreateAsync(_services, graph, state, input: input, cancellationToken: cancellationToken);
            if (bookmark == null)
            {
                ValidateInitialState(state, record, configuration);
                context.ScheduleWorkflow();
            }
            else if (context.ScheduleBookmark(bookmark) == null)
            {
                throw new InvalidOperationException("The trusted continuation could not be scheduled.");
            }
            var attemptId = Guid.NewGuid().ToString("N");
            record = await _store.PrepareStartAsync(record.Id, record.Revision, attemptId,
                bookmark == null ? null : record.CheckpointFingerprint, bookmark?.Id, cancellationToken)
                ?? throw new InvalidOperationException("Admission start preparation lost its authority boundary.");
            var prepared = _authorities.Capture(context, _activitySerializer, _extractor, _stateSerializer);
            await ObserveAsync(AdmissionExecutionBoundary.StartPrepared, record, cancellationToken);
            // No retry/readback capability: only this definitive first commit permits binding.
            record = await _store.AuthorizeStartAsync(record.Id, record.Revision, attemptId, cancellationToken)
                ?? throw new InvalidOperationException("Admission start authorization was not committed.");
            await ObserveAsync(AdmissionExecutionBoundary.StartAuthorized, record, cancellationToken);
            _authorities.Bind(context, prepared, record, configuration);
            RunWorkflowResult result;
            using (WorkflowExecutionScope.Begin(context))
            {
                await ObserveAsync(AdmissionExecutionBoundary.BeforeRunnerEntry, record, cancellationToken);
                result = await _runner.RunAsync(context);
                await ObserveAsync(AdmissionExecutionBoundary.RunnerReturned, record, cancellationToken);
                await _instances.SaveAsync(result.WorkflowState, cancellationToken);
                await ObserveAsync(AdmissionExecutionBoundary.TrailingWriteCompleted, record, cancellationToken);
            }
            _authorities.Retire(context);
            await ObserveAsync(AdmissionExecutionBoundary.OwnershipUnwound, record, cancellationToken);
            persisted = await _instances.FindByIdAsync(context.Id, cancellationToken)
                ?? throw new InvalidOperationException("The final admission state is missing.");
            if (_stateSerializer.Serialize(persisted.WorkflowState) != _stateSerializer.Serialize(result.WorkflowState))
            {
                throw new InvalidOperationException("The admission final write changed its expected state.");
            }
            var fingerprint = await CheckpointFingerprintAsync(persisted.WorkflowState, cancellationToken);
            record = await _store.CompleteExecutionAsync(record.Id, record.Revision, attemptId, fingerprint,
                persisted.WorkflowState.Bookmarks.Select(x => x.Id).ToArray(), persisted.Status == WorkflowStatus.Finished,
                _clock.UtcNow, cancellationToken) ?? throw new InvalidOperationException("The admission checkpoint was not recorded.");
        }
        catch
        {
            await MarkRecoveryAsync(record.Id, "execution-or-checkpoint-incomplete");
            throw;
        }
        finally
        {
            if (context != null)
            {
                _authorities.Retire(context);
            }
        }
        // Only a definitive CompleteExecutionAsync return crosses this boundary. An
        // observer/response failure afterward propagates without demoting a healthy
        // checkpoint; an unknown commit above still requires conservative recovery.
        await ObserveAsync(AdmissionExecutionBoundary.CheckpointRecorded, record, cancellationToken);
        return new()
        {
            WorkflowInstanceId = persisted.Id, Status = persisted.Status, SubStatus = persisted.SubStatus,
            Bookmarks = persisted.WorkflowState.Bookmarks, Incidents = persisted.WorkflowState.Incidents,
            Output = includeWorkflowOutput ? new Dictionary<string, object>(persisted.WorkflowState.Output) : null
        };
    }

    private async Task<WorkflowGraph> LoadPinnedGraphAsync(AdmissionSubscriptionConfiguration configuration, CancellationToken cancellationToken)
    {
        var definition = await _definitions.FindWorkflowDefinitionAsync(configuration.DefinitionVersionId, cancellationToken)
            ?? throw new InvalidOperationException("The pinned admission workflow is missing.");
        if (!definition.IsPublished || definition.DefinitionId != configuration.DefinitionId || definition.Version != configuration.DefinitionVersion ||
            definition.TenantId != configuration.TenantId || AdmissionDefinitionFingerprint.Compute(definition, _payloadSerializer) != configuration.DefinitionFingerprint)
        {
            throw new InvalidOperationException("The pinned admission definition changed.");
        }
        var graph = await _definitions.MaterializeWorkflowAsync(definition, cancellationToken);
        _host.ValidateGraph(graph);
        return graph;
    }

    private async Task<string> CheckpointFingerprintAsync(WorkflowState state, CancellationToken cancellationToken)
    {
        var stored = (await _bookmarks.FindManyAsync(new BookmarkFilter { WorkflowInstanceId = state.Id }, cancellationToken)).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        if (!state.Bookmarks.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(stored.Select(x => x.Id), StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The durable admission bookmark set is inconsistent.");
        }
        return AdmissionHash.Compute(JsonSerializer.Serialize(new[] { _stateSerializer.Serialize(state), _payloadSerializer.Serialize(stored) }));
    }

    private void ValidateScope(AdmissionSubscriptionConfiguration configuration)
    {
        if (configuration.TenantId != _host.TenantId || configuration.TenantId != _tenant.TenantId || configuration.EnvironmentId != _host.EnvironmentId)
        {
            throw new InvalidOperationException("The trusted admission scope does not match this isolated host.");
        }
    }
    private static AdmissionSubscriptionConfiguration Configuration(AdmissionRecord record) =>
        JsonSerializer.Deserialize<AdmissionSubscriptionConfiguration>(record.AdmittedConfigurationJson)
        ?? throw new InvalidOperationException("The admitted configuration snapshot is missing.");
    private void ValidateInitialState(WorkflowState state, AdmissionRecord record, AdmissionSubscriptionConfiguration configuration)
    {
        if (record.CheckpointFingerprint != AdmissionHash.Compute(_stateSerializer.Serialize(state)) ||
            state.Id != record.WorkflowInstanceId || state.DefinitionId != configuration.DefinitionId || state.DefinitionVersionId != configuration.DefinitionVersionId ||
            state.DefinitionVersion != configuration.DefinitionVersion || state.SubStatus != WorkflowSubStatus.Pending ||
            state.IsExecuting || state.Bookmarks.Count != 0 || state.ActivityExecutionContexts.Count != 0 || state.ScheduledActivities.Count != 0)
        {
            throw new InvalidOperationException("The materialized initial admission state changed.");
        }
    }
    private async Task<bool> IsCurrentOwnedSnapshotAsync(AdmissionRecord snapshot, CancellationToken cancellationToken)
    {
        var current = await FindRequiredAsync(snapshot.Id, cancellationToken);
        return current.Id == snapshot.Id && current.Revision == snapshot.Revision && current.State == snapshot.State &&
            current.WorkflowInstanceId == snapshot.WorkflowInstanceId;
    }
    private async Task<AdmissionRecord> FindRequiredAsync(string id, CancellationToken cancellationToken) =>
        await _store.FindAsync(id, cancellationToken) ?? throw new InvalidOperationException("The admission record is missing.");
    private ValueTask ObserveAsync(AdmissionExecutionBoundary boundary, AdmissionRecord record, CancellationToken cancellationToken) =>
        (_services.GetService(typeof(IAdmissionExecutionObserver)) as IAdmissionExecutionObserver)?.ObserveAsync(boundary, record.Id, record.WorkflowInstanceId!, cancellationToken) ?? ValueTask.CompletedTask;
    private async Task MarkRecoveryAsync(string id, string code)
    {
        try
        {
            var current = await _store.FindAsync(id, CancellationToken.None);
            if (current != null && current.State != AdmissionState.Terminal)
            {
                await _store.RequireRecoveryAsync(id, current.Revision, code, CancellationToken.None);
            }
        }
        catch
        {
            // Preserve the original exception. A durable unfinished phase is recoverable by
            // the bounded recovery scanner; this failure never creates new authority.
        }
    }
}
