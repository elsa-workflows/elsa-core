using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Tenants.Options;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Indexing of tenant-agnostic (<see cref="Tenant.AgnosticTenantId"/>) workflow definitions when multitenancy is enabled
/// (docs/adr/2026-11-20-tenant-agnostic-workflows-share-one-row-per-unscheduled-trigger.md):
/// each unscheduled trigger is stored once, stamped <c>*</c>, and each scheduled trigger once per active tenant, stamped with that tenant.
/// A registered <c>*</c> tenant gets no scheduled rows, and neither does a tenant that overrides the definition with a published definition
/// of its own with the same definition ID. Each set of rows is reconciled against its own existing rows only.
/// </summary>
public partial class TriggerIndexer
{
    // Reading in a "*" context returns exactly the tenant-agnostic rows: the visibility filter of every store admits the ambient tenant and "*".
    private static readonly Tenant SharedView = new() { Id = Tenant.AgnosticTenantId, Name = Tenant.AgnosticTenantId };

    private bool IsTenancyEnabled => _serviceProvider.GetService<IOptions<TenantsOptions>>()?.Value.IsEnabled == true;
    private ITenantAccessor TenantAccessor => _serviceProvider.GetRequiredService<ITenantAccessor>();
    private string CurrentTenantId => TenantAccessor.TenantId;
    private IWorkflowDefinitionStore WorkflowDefinitionStore => _serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
    private ISet<string> ScheduledTriggerNames => _serviceProvider.GetService<IOptions<RuntimeOptions>>()?.Value.ScheduledTriggerNames ?? new HashSet<string>();

    private bool IsSharedDefinition(Workflow workflow) => IsTenancyEnabled && workflow.Identity.TenantId == Tenant.AgnosticTenantId;

    /// <summary>
    /// Reconciles the trigger rows of tenant-agnostic workflow definitions for the tenant that is activating: the shared rows of every
    /// published tenant-agnostic definition version that is not in <paramref name="indexedVersionIds"/>, then this tenant's copies of them,
    /// then removes and unschedules this tenant's scheduled rows whose definition version is no longer published.
    /// </summary>
    /// <remarks>
    /// The triggers of a version are derived, and its shared rows reconciled, once per process (<see cref="SharedDefinitionTriggersCache"/>),
    /// so a tenant's activation costs a fixed number of queries however many tenants and definitions there are.
    /// </remarks>
    /// <param name="indexedVersionIds">The IDs of the definition versions that the activation has already indexed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async Task ReconcileSharedDefinitionsOnActivationAsync(IReadOnlySet<string> indexedVersionIds, CancellationToken cancellationToken)
    {
        if (!IsTenancyEnabled)
            return;

        // Activation re-derives rows from tenant-agnostic definitions that are already persisted, as the provider import does.
        using var grant = SharedTenantWriteScope.Enter<StoredTrigger>();
        List<WorkflowDefinitionSummary> sharedVersions;

        using (TenantAccessor.PushContext(SharedView))
            sharedVersions = (await WorkflowDefinitionStore.FindSummariesAsync(new WorkflowDefinitionFilter(), cancellationToken)).ToList();

        var publishedVersions = sharedVersions.Where(x => x.IsPublished && !indexedVersionIds.Contains(x.Id)).ToList();

        if (publishedVersions.Count > 0)
        {
            var derivedTriggers = await DeriveSharedTriggersAsync(publishedVersions, cancellationToken);

            // The shared rows exist before any tenant's own copies of them are removed.
            foreach (var derived in derivedTriggers)
                await EnsureSharedRowsReconciledAsync(derived, cancellationToken);

            await ReconcileTenantCopiesOnActivationAsync(derivedTriggers, sharedVersions.Select(x => x.Id).ToHashSet(), cancellationToken);
        }

        await RemoveUnpublishedScheduledTriggersAsync(cancellationToken);
    }

    private SharedDefinitionTriggersCache SharedTriggersCache => _serviceProvider.GetService<SharedDefinitionTriggersCache>() ?? (_fallbackSharedTriggersCache ??= new());
    private SharedDefinitionTriggersCache? _fallbackSharedTriggersCache;

    /// <summary>
    /// Returns the triggers of the published versions, deriving those that this process has not derived yet. A version that cannot be
    /// derived is logged and skipped.
    /// </summary>
    private async Task<List<SharedDefinitionTriggersCache.DerivedTriggers>> DeriveSharedTriggersAsync(IReadOnlyCollection<WorkflowDefinitionSummary> publishedVersions, CancellationToken cancellationToken)
    {
        var cache = SharedTriggersCache;
        var cacheManager = _serviceProvider.GetService<ICacheManager>();
        var missingVersionIds = publishedVersions.Select(x => x.Id).Where(x => !cache.ContainsDerived(x, cacheManager)).ToList();
        var definitions = new Dictionary<string, WorkflowDefinition>();

        if (missingVersionIds.Count > 0)
        {
            using (TenantAccessor.PushContext(SharedView))
                definitions = (await WorkflowDefinitionStore.FindManyAsync(new WorkflowDefinitionFilter { Ids = missingVersionIds }, cancellationToken)).ToDictionary(x => x.Id);
        }

        var result = new List<SharedDefinitionTriggersCache.DerivedTriggers>();

        foreach (var version in publishedVersions)
        {
            try
            {
                result.Add(await cache.GetOrDeriveAsync(version.Id, () => DeriveAsync(version.Id, definitions.GetValueOrDefault(version.Id)), cacheManager));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Failed to derive the triggers of tenant-agnostic workflow definition {WorkflowDefinitionId}.", version.DefinitionId);
            }
        }

        return result;

        async Task<SharedDefinitionTriggersCache.DerivedTriggers> DeriveAsync(string versionId, WorkflowDefinition? definition)
        {
            if (definition == null)
            {
                using (TenantAccessor.PushContext(SharedView))
                    definition = await WorkflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = versionId }, cancellationToken);
            }

            if (definition == null)
                throw new InvalidOperationException($"Workflow definition version {versionId} was not found.");

            var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
            var scheduledNames = ScheduledTriggerNames;
            var triggers = await GetTriggersInternalAsync(workflowGraph.Workflow, cancellationToken).ToListAsync(cancellationToken);
            var sharedTriggers = triggers.Where(x => !IsScheduled(x, scheduledNames)).Select(x => CopyForTenant(x, Tenant.AgnosticTenantId)).ToList();
            var scheduledTriggers = triggers.Where(x => IsScheduled(x, scheduledNames)).ToList();
            return new(workflowGraph.Workflow, sharedTriggers, scheduledTriggers);
        }
    }

    /// <summary>
    /// Reconciles the shared rows of the version once per process. A failure is logged, and the next activation tries again.
    /// </summary>
    private async Task EnsureSharedRowsReconciledAsync(SharedDefinitionTriggersCache.DerivedTriggers derived, CancellationToken cancellationToken)
    {
        var workflow = derived.Workflow;

        try
        {
            await SharedTriggersCache.EnsureSharedRowsReconciledAsync(workflow.Identity.Id, async () =>
            {
                await using (await AcquireDefinitionLockAsync(workflow.Identity.DefinitionId, cancellationToken))
                    await ReconcileSharedTriggersAsync(workflow, derived.SharedTriggers.Select(x => CopyForTenant(x, Tenant.AgnosticTenantId)).ToList(), cancellationToken);

                return true;
            }, _serviceProvider.GetService<ICacheManager>());
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Failed to index the shared triggers of tenant-agnostic workflow definition {WorkflowDefinitionId}.", workflow.Identity.DefinitionId);
        }
    }

    /// <summary>
    /// Reconciles the activating tenant's copies of the published tenant-agnostic definition versions in one pass: one query for its rows
    /// of those definitions and, only when there are scheduled triggers to copy, one for the definitions it overrides.
    /// </summary>
    /// <param name="derivedTriggers">The triggers of the published versions.</param>
    /// <param name="sharedVersionIds">The IDs of all versions of the tenant-agnostic definitions. A tenant's row that names one is a copy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ReconcileTenantCopiesOnActivationAsync(IReadOnlyCollection<SharedDefinitionTriggersCache.DerivedTriggers> derivedTriggers, IReadOnlySet<string> sharedVersionIds, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId;
        var scheduledNames = ScheduledTriggerNames;
        var definitionIds = derivedTriggers.Select(x => x.Workflow.Identity.DefinitionId).Distinct().ToList();
        var copies = (await _triggerStore.FindManyAsync(new TriggerFilter { WorkflowDefinitionIds = definitionIds }, cancellationToken))
            .Where(x => IsTenantRow(x, tenantId, scheduledNames) && sharedVersionIds.Contains(x.WorkflowDefinitionVersionId))
            .ToLookup(x => x.WorkflowDefinitionId);
        var withScheduledTriggers = derivedTriggers.Where(x => x.ScheduledTriggers.Count > 0).Select(x => x.Workflow.Identity.DefinitionId).Distinct().ToList();
        var overriddenDefinitionIds = new HashSet<string>();

        if (tenantId != Tenant.AgnosticTenantId && withScheduledTriggers.Count > 0)
        {
            var filter = new WorkflowDefinitionFilter { DefinitionIds = withScheduledTriggers, VersionOptions = VersionOptions.Published };
            var publishedVersions = await WorkflowDefinitionStore.FindSummariesAsync(filter, cancellationToken);
            overriddenDefinitionIds = publishedVersions.Where(x => !sharedVersionIds.Contains(x.Id)).Select(x => x.DefinitionId).ToHashSet();
        }

        foreach (var derived in derivedTriggers)
        {
            var definitionId = derived.Workflow.Identity.DefinitionId;
            var currentTriggers = copies[definitionId].ToList();
            var newTriggers = tenantId == Tenant.AgnosticTenantId || overriddenDefinitionIds.Contains(definitionId)
                ? []
                : derived.ScheduledTriggers.Select(x => CopyForTenant(x, tenantId)).ToList();

            if (currentTriggers.Count == 0 && newTriggers.Count == 0)
                continue;

            var diff = Diff.For(currentTriggers, newTriggers, _triggerEqualityComparer);

            if (diff.Added.Count == 0 && diff.Removed.Count == 0)
                continue;

            await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);
            await _notificationSender.SendAsync(new WorkflowTriggersIndexed(new(derived.Workflow, diff.Added, diff.Removed, diff.Unchanged)), cancellationToken);
        }
    }

    /// <summary>
    /// After the current tenant deleted versions of its own workflow definitions, reconciles its scheduled rows of the published
    /// tenant-agnostic definitions with the same IDs, which the tenant may have stopped overriding.
    /// </summary>
    internal async Task ReconcileTenantCopiesAfterDeletionAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken)
    {
        if (!IsTenancyEnabled || CurrentTenantId == Tenant.AgnosticTenantId)
            return;

        foreach (var definitionId in definitionIds.Distinct())
        {
            await using (await AcquireDefinitionLockAsync(definitionId, cancellationToken))
            {
                // Its existing copies are replaced, not kept: the deletion may already have unscheduled them.
                await ReconcileTenantCopiesOfPublishedSharedDefinitionAsync(definitionId, true, cancellationToken);
            }
        }

        // While the tenant overrode a definition, its lookups did not return the tenant-agnostic rows of that definition.
        if (_serviceProvider.GetService<ICacheManager>() is { } cacheManager)
            await cacheManager.TriggerTokenAsync(CachingTriggerStore.CacheInvalidationTokenKey, cancellationToken);
    }

    private async Task<IndexedWorkflowTriggers> IndexSharedDefinitionTriggersAsync(Workflow workflow, bool allTenants, CancellationToken cancellationToken)
    {
        var scheduledNames = ScheduledTriggerNames;
        var triggers = workflow.Publication.IsPublished
            ? await GetTriggersInternalAsync(workflow, cancellationToken).ToListAsync(cancellationToken)
            : [];
        var sharedTriggers = triggers.Where(x => !IsScheduled(x, scheduledNames)).ToList();
        var scheduledTriggers = triggers.Where(x => IsScheduled(x, scheduledNames)).ToList();

        foreach (var trigger in sharedTriggers)
            trigger.TenantId = Tenant.AgnosticTenantId;

        // What activations remember about this definition's versions no longer holds; the provider import (one tenant) changes nothing.
        if (allTenants)
            await SharedTriggersCache.InvalidateAsync(_serviceProvider.GetService<ICacheManager>(), cancellationToken);

        // The shared rows exist before any tenant's own copies of them are removed.
        var sharedResult = await ReconcileSharedTriggersAsync(workflow, sharedTriggers, cancellationToken);
        var tenantResult = allTenants
            ? await ReconcileTenantCopiesForAllTenantsAsync(workflow, scheduledTriggers, cancellationToken)
            : await ReconcileTenantCopiesAsync(workflow, scheduledTriggers, false, cancellationToken);

        if (tenantResult == null)
            return sharedResult;

        return new(
            workflow,
            sharedResult.AddedTriggers.Concat(tenantResult.AddedTriggers).ToList(),
            sharedResult.RemovedTriggers.Concat(tenantResult.RemovedTriggers).ToList(),
            sharedResult.UnchangedTriggers.Concat(tenantResult.UnchangedTriggers).ToList());
    }

    /// <summary>
    /// Replaces the shared rows of the definition. Writing them requires a <see cref="SharedTenantWriteScope"/> for <see cref="StoredTrigger"/>,
    /// which the caller holds.
    /// </summary>
    private async Task<IndexedWorkflowTriggers> ReconcileSharedTriggersAsync(Workflow workflow, ICollection<StoredTrigger> triggers, CancellationToken cancellationToken)
    {
        var currentTriggers = await FindSharedTriggersAsync(new() { WorkflowDefinitionId = workflow.Identity.DefinitionId }, cancellationToken);
        var diff = Diff.For(currentTriggers, triggers, _triggerEqualityComparer);
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, diff.Added, diff.Removed, diff.Unchanged);
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
        return indexedWorkflow;
    }

    /// <summary>
    /// Reconciles the scheduled rows of the shared definition for every active tenant, each in its own tenant scope, so that the trigger
    /// scheduler schedules and unschedules there. Returns the result for the current tenant.
    /// </summary>
    private async Task<IndexedWorkflowTriggers?> ReconcileTenantCopiesForAllTenantsAsync(Workflow workflow, IReadOnlyCollection<StoredTrigger> scheduledTriggers, CancellationToken cancellationToken)
    {
        var currentTenantId = CurrentTenantId;
        IndexedWorkflowTriggers? currentTenantResult = null;

        await ForEachTenantAsync(async (indexer, tenantId) =>
        {
            var result = await indexer.ReconcileTenantCopiesAsync(workflow, scheduledTriggers, false, cancellationToken);

            if (tenantId == currentTenantId)
                currentTenantResult = result;
        }, false, false, cancellationToken);

        return currentTenantResult;
    }

    /// <summary>
    /// Reconciles the current tenant's copies of the shared definition: its rows of the definition ID that do not belong to a version of
    /// its own definition with the same ID. They become one row per scheduled trigger, unless the definition is not published, the tenant
    /// overrides it, or the tenant is a registered <c>*</c> tenant.
    /// </summary>
    /// <returns>The result, or <c>null</c> when the tenant has no copies and gets none.</returns>
    private async Task<IndexedWorkflowTriggers?> ReconcileTenantCopiesAsync(Workflow sharedWorkflow, IReadOnlyCollection<StoredTrigger> scheduledTriggers, bool replaceAll, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId;
        var scheduledNames = ScheduledTriggerNames;
        var definitionId = sharedWorkflow.Identity.DefinitionId;
        var filter = new TriggerFilter { WorkflowDefinitionId = definitionId };
        var ownTriggers = (await _triggerStore.FindManyAsync(filter, cancellationToken)).Where(x => IsOwnedBy(x, tenantId)).ToList();
        List<StoredTrigger> currentTriggers;
        List<StoredTrigger> newTriggers;

        if (tenantId == Tenant.AgnosticTenantId)
        {
            // Rows of a registered "*" tenant would fire in a "*" context, and Elsa does not persist "*" instances.
            currentTriggers = ownTriggers.Where(x => IsScheduled(x, scheduledNames)).ToList();
            newTriggers = [];
        }
        else
        {
            if (ownTriggers.Count == 0 && scheduledTriggers.Count == 0)
                return null;

            var ownVersions = await FindOwnVersionsAsync(definitionId, cancellationToken);
            var ownVersionIds = ownVersions.Select(x => x.Id).ToHashSet();
            var overrides = ownVersions.Any(x => x.IsPublished);
            currentTriggers = ownTriggers.Where(x => !ownVersionIds.Contains(x.WorkflowDefinitionVersionId)).ToList();
            newTriggers = sharedWorkflow.Publication.IsPublished && !overrides
                ? scheduledTriggers.Select(x => CopyForTenant(x, tenantId)).ToList()
                : [];
        }

        if (currentTriggers.Count == 0 && newTriggers.Count == 0)
            return null;

        var diff = replaceAll ? Diff.From<StoredTrigger>(newTriggers, currentTriggers, []) : Diff.For(currentTriggers, newTriggers, _triggerEqualityComparer);
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var indexedWorkflow = new IndexedWorkflowTriggers(sharedWorkflow, diff.Added, diff.Removed, diff.Unchanged);
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
        return indexedWorkflow;
    }

    /// <summary>
    /// Reconciles the current tenant's copies of the published tenant-agnostic definition with the specified ID, if there is one.
    /// The caller holds the indexing lock of the definition ID.
    /// </summary>
    private async Task ReconcileTenantCopiesOfPublishedSharedDefinitionAsync(string definitionId, bool replaceAll, CancellationToken cancellationToken)
    {
        if (CurrentTenantId == Tenant.AgnosticTenantId)
            return;

        WorkflowDefinition? definition;

        using (TenantAccessor.PushContext(SharedView))
            definition = await WorkflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Published }, cancellationToken);

        if (definition == null)
            return;

        var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var scheduledNames = ScheduledTriggerNames;
        var scheduledTriggers = (await GetTriggersInternalAsync(workflowGraph.Workflow, cancellationToken).ToListAsync(cancellationToken))
            .Where(x => IsScheduled(x, scheduledNames))
            .ToList();
        await ReconcileTenantCopiesAsync(workflowGraph.Workflow, scheduledTriggers, replaceAll, cancellationToken);
    }

    /// <summary>
    /// Removes and unschedules the current tenant's scheduled rows whose definition version is no longer published, for example because a
    /// tenant-agnostic definition was retracted or deleted while the tenant was inactive.
    /// </summary>
    private async Task RemoveUnpublishedScheduledTriggersAsync(CancellationToken cancellationToken)
    {
        var scheduledNames = ScheduledTriggerNames;

        if (scheduledNames.Count == 0)
            return;

        var tenantId = CurrentTenantId;
        var filter = new TriggerFilter { Names = scheduledNames.ToList() };
        var triggers = (await _triggerStore.FindManyAsync(filter, cancellationToken)).Where(x => IsOwnedBy(x, tenantId)).ToList();

        if (triggers.Count == 0)
            return;

        var versionIds = triggers.Select(x => x.WorkflowDefinitionVersionId).Distinct().ToList();
        var definitionFilter = new WorkflowDefinitionFilter { Ids = versionIds, VersionOptions = VersionOptions.Published };
        var publishedVersionIds = (await WorkflowDefinitionStore.FindSummariesAsync(definitionFilter, cancellationToken)).Select(x => x.Id).ToHashSet();

        foreach (var group in triggers.Where(x => !publishedVersionIds.Contains(x.WorkflowDefinitionVersionId)).GroupBy(x => x.WorkflowDefinitionVersionId))
        {
            var removed = group.ToList();
            var workflow = await FindWorkflowAsync(group.Key, removed[0].WorkflowDefinitionId, cancellationToken);
            await RemoveTriggersAsync(workflow, removed, false, cancellationToken);
        }
    }

    /// <summary>
    /// Returns the rows that a deletion in the current tenant may remove: its own rows and, when the caller may delete tenant-agnostic
    /// workflow definitions, the shared rows.
    /// </summary>
    private async Task<List<StoredTrigger>> FindTriggersToDeleteAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);

        if (!IsTenancyEnabled)
            return triggers.ToList();

        var tenantId = CurrentTenantId;
        var scheduledNames = ScheduledTriggerNames;
        var result = triggers.Where(x => IsTenantRow(x, tenantId, scheduledNames)).ToList();

        if (SharedTenantWriteScope.IsGranted(typeof(WorkflowDefinition)))
            result.AddRange(await FindSharedTriggersAsync(filter, cancellationToken));

        return result;
    }

    /// <summary>
    /// When tenant-agnostic definition versions are deleted, removes and unschedules the copies of their triggers of every other active
    /// tenant. Only a caller with the authority to delete tenant-agnostic definitions deletes them.
    /// </summary>
    private async Task DeleteTenantCopiesOfDeletedSharedDefinitionsAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        if (!IsTenancyEnabled || !SharedTenantWriteScope.IsGranted(typeof(WorkflowDefinition)))
            return;

        await SharedTriggersCache.InvalidateAsync(_serviceProvider.GetService<ICacheManager>(), cancellationToken);

        var definitionFilter = new WorkflowDefinitionFilter
        {
            Id = filter.WorkflowDefinitionVersionId,
            Ids = filter.WorkflowDefinitionVersionIds,
            DefinitionId = filter.WorkflowDefinitionId,
            DefinitionIds = filter.WorkflowDefinitionIds
        };

        if (definitionFilter is { Id: null, Ids: null, DefinitionId: null, DefinitionIds: null })
            return;

        IEnumerable<WorkflowDefinitionSummary> sharedVersions;

        using (TenantAccessor.PushContext(SharedView))
            sharedVersions = await WorkflowDefinitionStore.FindSummariesAsync(definitionFilter, cancellationToken);

        var copyFilter = new TriggerFilter { WorkflowDefinitionVersionIds = sharedVersions.Select(x => x.Id).ToList() };

        if (copyFilter.WorkflowDefinitionVersionIds.Count == 0)
            return;

        await ForEachTenantAsync(async (indexer, tenantId) =>
        {
            var scheduledNames = indexer.ScheduledTriggerNames;
            var copies = (await indexer._triggerStore.FindManyAsync(copyFilter, cancellationToken)).Where(x => IsTenantRow(x, tenantId, scheduledNames));

            foreach (var group in copies.GroupBy(x => x.WorkflowDefinitionVersionId))
            {
                var removed = group.ToList();
                var workflow = await indexer.FindWorkflowAsync(group.Key, removed[0].WorkflowDefinitionId, cancellationToken);
                await indexer.RemoveTriggersAsync(workflow, removed, filter.RequireCompleteCleanup, cancellationToken);
            }
        }, true, filter.RequireCompleteCleanup, cancellationToken);
    }

    private async Task RemoveTriggersAsync(Workflow workflow, List<StoredTrigger> removed, bool requireCompleteCleanup, CancellationToken cancellationToken)
    {
        var indexed = new IndexedWorkflowTriggers(workflow, [], removed, []);
        var notification = new WorkflowTriggersIndexed(indexed);

        if (requireCompleteCleanup)
        {
            // Keep the removal payload available for retry if required cleanup fails.
            await _notificationSender.SendAsync(notification, NotificationStrategy.Sequential, cancellationToken);
            await _triggerStore.ReplaceAsync(removed, [], cancellationToken);
        }
        else
        {
            await _triggerStore.ReplaceAsync(removed, [], cancellationToken);
            await _notificationSender.SendAsync(notification, cancellationToken);
        }
    }

    /// <summary>
    /// Returns the shared rows that match the filter: the rows stamped <c>*</c> whose trigger is unscheduled. A tenant's lookups hide them
    /// while the tenant overrides their definition, so they are read in a <c>*</c> context.
    /// </summary>
    private async Task<List<StoredTrigger>> FindSharedTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        IEnumerable<StoredTrigger> triggers;

        using (TenantAccessor.PushContext(SharedView))
            triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);

        var scheduledNames = ScheduledTriggerNames;
        return triggers.Where(x => x.TenantId == Tenant.AgnosticTenantId && !IsScheduled(x, scheduledNames)).ToList();
    }

    /// <summary>
    /// Returns the versions of the current tenant's own definition with the specified ID, without those of the tenant-agnostic definition.
    /// </summary>
    private async Task<List<WorkflowDefinitionSummary>> FindOwnVersionsAsync(string definitionId, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var store = WorkflowDefinitionStore;
        var visibleVersions = await store.FindSummariesAsync(filter, cancellationToken);
        IEnumerable<WorkflowDefinitionSummary> sharedVersions;

        using (TenantAccessor.PushContext(SharedView))
            sharedVersions = await store.FindSummariesAsync(filter, cancellationToken);

        var sharedVersionIds = sharedVersions.Select(x => x.Id).ToHashSet();
        return visibleVersions.Where(x => !sharedVersionIds.Contains(x.Id)).ToList();
    }

    private async Task<Workflow> FindWorkflowAsync(string workflowDefinitionVersionId, string workflowDefinitionId, CancellationToken cancellationToken)
    {
        var workflowGraph = await _workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionVersionId, cancellationToken);

        // A deleted version has no graph; the removal notification only needs its identity.
        return workflowGraph?.Workflow ?? new Workflow
        {
            Identity = new(workflowDefinitionId, 0, workflowDefinitionVersionId),
            Publication = new(false, false)
        };
    }

    /// <summary>
    /// Runs the action for every active tenant in a scope of that tenant, with a trigger indexer of that scope.
    /// </summary>
    private async Task ForEachTenantAsync(Func<TriggerIndexer, string, Task> action, bool excludeCurrent, bool throwOnFailure, CancellationToken cancellationToken)
    {
        var tenantService = _serviceProvider.GetService<ITenantService>();
        var tenantScopeFactory = _serviceProvider.GetService<ITenantScopeFactory>();
        var currentTenantId = CurrentTenantId;

        if (tenantService == null || tenantScopeFactory == null)
        {
            if (!excludeCurrent)
                await action(this, currentTenantId);

            return;
        }

        foreach (var tenant in await tenantService.ListAsync(cancellationToken))
        {
            var tenantId = tenant.Id.NormalizeTenantId();

            if (excludeCurrent && tenantId == currentTenantId)
                continue;

            try
            {
                await using var scope = tenantScopeFactory.CreateScope(tenant);
                var indexer = scope.ServiceProvider.GetService<ITriggerIndexer>() as TriggerIndexer ?? ActivatorUtilities.CreateInstance<TriggerIndexer>(scope.ServiceProvider);
                await action(indexer, tenantId);
            }
            catch (Exception e) when (!throwOnFailure && e is not OperationCanceledException)
            {
                _logger.LogError(e, "Failed to reconcile the triggers of tenant-agnostic workflow definitions for tenant {TenantId}; its next activation reconciles them again.", tenant.Id);
            }
        }
    }

    private StoredTrigger CopyForTenant(StoredTrigger trigger, string tenantId) => new()
    {
        Id = _identityGenerator.GenerateId(),
        WorkflowDefinitionId = trigger.WorkflowDefinitionId,
        WorkflowDefinitionVersionId = trigger.WorkflowDefinitionVersionId,
        Name = trigger.Name,
        ActivityId = trigger.ActivityId,
        Hash = trigger.Hash,
        Payload = trigger.Payload,
        TenantId = tenantId
    };

    private static bool IsScheduled(StoredTrigger trigger, ISet<string> scheduledNames) => trigger.Name != null && scheduledNames.Contains(trigger.Name);

    private static bool IsOwnedBy(StoredTrigger trigger, string tenantId) => trigger.TenantId.NormalizeTenantId() == tenantId;

    /// <summary>
    /// Returns whether the row belongs to the tenant rather than to all tenants. The rows stamped <c>*</c> of a registered <c>*</c> tenant
    /// are its scheduled rows; its unscheduled ones are the shared rows.
    /// </summary>
    private static bool IsTenantRow(StoredTrigger trigger, string tenantId, ISet<string> scheduledNames) =>
        IsOwnedBy(trigger, tenantId) && (tenantId != Tenant.AgnosticTenantId || IsScheduled(trigger, scheduledNames));
}
