using Elsa.Caching;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
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
using Elsa.Workflows.Runtime.Services;
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
    /// <para>
    /// On the shared database, the triggers of a version are derived, and its shared rows reconciled, once per process
    /// (<see cref="SharedDefinitionTriggersCache"/>). When nothing needs changing for the tenant, its activation costs a fixed number of
    /// queries however many tenants and definitions there are.
    /// </para>
    /// <para>
    /// Each definition whose copies do need changing for the tenant (copies to add or to remove) costs more: the definition's lock, a
    /// publication check and a re-read of the tenant's versions and rows of it. So does a tenant with its own database, which derives and
    /// reconciles every definition itself.
    /// </para>
    /// </remarks>
    /// <param name="indexedVersionIds">The IDs of the definition versions that the activation has already indexed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async Task ReconcileSharedDefinitionsOnActivationAsync(IReadOnlySet<string> indexedVersionIds, CancellationToken cancellationToken)
    {
        if (!IsTenancyEnabled)
            return;

        // Activation re-derives rows from tenant-agnostic definitions that are already persisted, as the provider import does.
        using var grant = SharedTenantWriteScope.Enter<StoredTrigger>();

        // The tenant sees its own definitions and the tenant-agnostic ones.
        var visiblePublishedVersions = (await WorkflowDefinitionStore.FindSummariesAsync(new WorkflowDefinitionFilter { VersionOptions = VersionOptions.Published }, cancellationToken)).ToList();
        var publishedVersions = visiblePublishedVersions.Where(x => x.TenantId == Tenant.AgnosticTenantId && !indexedVersionIds.Contains(x.Id)).ToList();

        if (publishedVersions.Count > 0)
        {
            // What a tenant with its own database derives and writes concerns that database only, so it is not shared with other tenants.
            var shareWork = TenantAccessor.Tenant?.HasOwnDatabase() != true;
            var derivedTriggers = await DeriveSharedTriggersAsync(publishedVersions, shareWork, cancellationToken);

            // The shared rows exist before any tenant's own copies of them are removed, so a definition whose shared rows could not be
            // written keeps the tenant's copies until a later attempt.
            var reconciledTriggers = new List<SharedDefinitionTriggersCache.DerivedTriggers>();

            foreach (var derived in derivedTriggers)
            {
                if (await EnsureSharedRowsReconciledAsync(derived, shareWork, cancellationToken))
                    reconciledTriggers.Add(derived);
            }

            var ownPublishedVersions = visiblePublishedVersions.Where(x => x.TenantId != Tenant.AgnosticTenantId).ToList();
            await ReconcileTenantCopiesOnActivationAsync(reconciledTriggers, ownPublishedVersions, shareWork, cancellationToken);
        }

        await RemoveUnpublishedScheduledTriggersAsync(cancellationToken);
    }

    private SharedDefinitionTriggersCache SharedTriggersCache => _serviceProvider.GetService<SharedDefinitionTriggersCache>() ?? (_fallbackSharedTriggersCache ??= new());
    private SharedDefinitionTriggersCache? _fallbackSharedTriggersCache;

    /// <summary>
    /// Returns the triggers of the published versions, deriving those that this process has not derived yet. A version that cannot be
    /// derived is logged and skipped.
    /// </summary>
    /// <param name="publishedVersions">The published versions.</param>
    /// <param name="shareWork">Whether the triggers are derived once per process for all tenants, or here for this tenant only.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<SharedDefinitionTriggersCache.DerivedTriggers>> DeriveSharedTriggersAsync(IReadOnlyCollection<WorkflowDefinitionSummary> publishedVersions, bool shareWork, CancellationToken cancellationToken)
    {
        var cache = SharedTriggersCache;
        var cacheManager = _serviceProvider.GetService<ICacheManager>();
        var missingVersionIds = publishedVersions.Select(x => x.Id).Where(x => !shareWork || !cache.ContainsDerived(x, cacheManager)).ToList();
        var definitions = new Dictionary<string, WorkflowDefinition>();

        // Taken before the definitions are read: a derivation from a read that the version was forgotten after is not shared.
        var generations = publishedVersions.ToDictionary(x => x.Id, x => cache.GetGeneration(x.Id));

        if (missingVersionIds.Count > 0)
            definitions = (await WorkflowDefinitionStore.FindManyAsync(new WorkflowDefinitionFilter { Ids = missingVersionIds }, cancellationToken)).ToDictionary(x => x.Id);

        var result = new List<SharedDefinitionTriggersCache.DerivedTriggers>();

        foreach (var version in publishedVersions)
        {
            try
            {
                var definition = definitions.GetValueOrDefault(version.Id);
                result.Add(shareWork
                    ? await cache.GetOrDeriveAsync(version.Id, generations[version.Id], sharedToken => DeriveTriggersAsync(version.Id, definition, sharedToken), cacheManager, cancellationToken)
                    : await DeriveTriggersAsync(version.Id, definition, cancellationToken));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Failed to derive the triggers of tenant-agnostic workflow definition {WorkflowDefinitionId}.", version.DefinitionId);
            }
        }

        return result;
    }

    private async Task<SharedDefinitionTriggersCache.DerivedTriggers> DeriveTriggersAsync(string versionId, WorkflowDefinition? definition, CancellationToken cancellationToken)
    {
        definition ??= await WorkflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = versionId }, cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Workflow definition version {versionId} was not found.");

        var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var scheduledNames = ScheduledTriggerNames;
        var triggers = await GetTriggersInternalAsync(workflowGraph.Workflow, cancellationToken).ToListAsync(cancellationToken);
        var sharedTriggers = triggers.Where(x => !IsScheduled(x, scheduledNames)).Select(x => CopyForTenant(x, Tenant.AgnosticTenantId)).ToList();
        var scheduledTriggers = triggers.Where(x => IsScheduled(x, scheduledNames)).ToList();
        return new(workflowGraph.Workflow, sharedTriggers, scheduledTriggers);
    }

    /// <summary>
    /// Returns the current triggers of the version, for use under the definition's lock. A provider import that changed the version in
    /// place and saved it before the lock was taken has made this process forget the earlier derivation, so the triggers are derived again
    /// from the saved definition; a tenant with its own database always derives them again.
    /// </summary>
    private Task<SharedDefinitionTriggersCache.DerivedTriggers> DeriveCurrentTriggersAsync(string versionId, bool shareWork, CancellationToken cancellationToken) =>
        shareWork
            ? SharedTriggersCache.GetOrDeriveAsync(versionId, SharedTriggersCache.GetGeneration(versionId), token => DeriveTriggersAsync(versionId, null, token), _serviceProvider.GetService<ICacheManager>(), cancellationToken)
            : DeriveTriggersAsync(versionId, null, cancellationToken);

    /// <summary>
    /// Reconciles the shared rows of the version once per process and returns whether they are in place. A version that is no longer
    /// published by the time the definition's lock is held is skipped: whatever retracted or replaced it reconciled the rows. A failure is
    /// logged, and the next activation tries again.
    /// </summary>
    /// <param name="derived">The triggers of the version.</param>
    /// <param name="shareWork">Whether this is done once per process for all tenants, or here for this tenant's database only.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<bool> EnsureSharedRowsReconciledAsync(SharedDefinitionTriggersCache.DerivedTriggers derived, bool shareWork, CancellationToken cancellationToken)
    {
        var workflow = derived.Workflow;

        try
        {
            return shareWork
                ? await SharedTriggersCache.EnsureSharedRowsReconciledAsync(workflow.Identity.Id, ReconcileAsync, _serviceProvider.GetService<ICacheManager>(), cancellationToken)
                : await ReconcileAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Failed to index the shared triggers of tenant-agnostic workflow definition {WorkflowDefinitionId}.", workflow.Identity.DefinitionId);
            return false;
        }

        async Task<bool> ReconcileAsync(CancellationToken token)
        {
            await using (await AcquireDefinitionLockAsync(workflow.Identity.DefinitionId, token))
            {
                // Retracted or replaced since it was listed: whatever did that reconciled the rows.
                if (!await IsPublishedAsync(workflow.Identity.Id, token))
                    return false;

                var current = await DeriveCurrentTriggersAsync(workflow.Identity.Id, shareWork, token);
                await ReconcileSharedTriggersAsync(current.Workflow, current.SharedTriggers.Select(x => CopyForTenant(x, Tenant.AgnosticTenantId)).ToList(), token);
                return true;
            }
        }
    }

    private Task<bool> IsPublishedAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken) =>
        WorkflowDefinitionStore.AnyAsync(new WorkflowDefinitionFilter { Id = workflowDefinitionVersionId, VersionOptions = VersionOptions.Published }, cancellationToken);

    /// <summary>
    /// Forgets what this process derived from the tenant-agnostic definition version, after a provider import changed it in place.
    /// </summary>
    internal void ForgetSharedDefinitionVersion(string workflowDefinitionVersionId) => SharedTriggersCache.Forget(workflowDefinitionVersionId);

    /// <summary>
    /// Reconciles the activating tenant's copies of the published tenant-agnostic definition versions with one query for its rows of
    /// those definitions. A row of the tenant is a copy unless it names a published version of the tenant's own definition with the same
    /// ID, which overrides the tenant-agnostic one.
    /// </summary>
    /// <param name="derivedTriggers">The triggers of the published versions.</param>
    /// <param name="ownPublishedVersions">The published versions of the tenant's own definitions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="shareWork">Whether the triggers of a version are derived once per process for all tenants.</param>
    private async Task ReconcileTenantCopiesOnActivationAsync(IReadOnlyCollection<SharedDefinitionTriggersCache.DerivedTriggers> derivedTriggers, IReadOnlyCollection<WorkflowDefinitionSummary> ownPublishedVersions, bool shareWork, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId;
        var scheduledNames = ScheduledTriggerNames;
        var definitionIds = derivedTriggers.Select(x => x.Workflow.Identity.DefinitionId).Distinct().ToList();
        var ownVersionIds = ownPublishedVersions.Select(x => x.Id).ToHashSet();
        var overriddenDefinitionIds = ownPublishedVersions.Select(x => x.DefinitionId).ToHashSet();
        var copies = (await _triggerStore.FindManyAsync(new TriggerFilter { WorkflowDefinitionIds = definitionIds }, cancellationToken))
            .Where(x => IsTenantRow(x, tenantId, scheduledNames) && !ownVersionIds.Contains(x.WorkflowDefinitionVersionId))
            .ToLookup(x => x.WorkflowDefinitionId);

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

            // The tenant may publish a definition of its own with this ID meanwhile: its publication removes the tenant's copies under this
            // lock, and its own timer would look like a copy in the rows read above. So copies are added and removed under the lock too,
            // against the tenant's versions and rows as they are now.
            await using (await AcquireDefinitionLockAsync(definitionId, cancellationToken))
            {
                // Retracted or replaced since it was listed: whatever did that reconciled the copies.
                if (!await IsPublishedAsync(derived.Workflow.Identity.Id, cancellationToken))
                    continue;

                var current = await DeriveCurrentTriggersAsync(derived.Workflow.Identity.Id, shareWork, cancellationToken);
                await ReconcileTenantCopiesAsync(current.Workflow, current.ScheduledTriggers, false, cancellationToken);
            }
        }
    }

    /// <summary>
    /// After the current tenant deleted versions of its own workflow definitions, reconciles its scheduled rows of the published
    /// tenant-agnostic definitions with the same IDs, which the tenant may have stopped overriding.
    /// </summary>
    /// <param name="definitionIds">The IDs of the definitions whose versions were deleted.</param>
    /// <param name="replaceExistingCopies">
    /// Whether the tenant's existing copies are replaced rather than kept when unchanged. A deletion by definition ID unschedules every
    /// scheduled row of that ID in the tenant, the copies included, so they must be scheduled again; deleting versions leaves them alone.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async Task ReconcileTenantCopiesAfterDeletionAsync(IEnumerable<string> definitionIds, bool replaceExistingCopies, CancellationToken cancellationToken)
    {
        if (!IsTenancyEnabled || CurrentTenantId == Tenant.AgnosticTenantId)
            return;

        foreach (var definitionId in definitionIds.Distinct())
        {
            await using (await AcquireDefinitionLockAsync(definitionId, cancellationToken))
            {
                await ReconcileTenantCopiesOfPublishedSharedDefinitionAsync(definitionId, replaceExistingCopies, cancellationToken);
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
    /// <remarks>
    /// Every row stamped <c>*</c> counts, whatever its name. A scheduled trigger is never stored stamped <c>*</c>, so such a row is removed:
    /// it is a registered <c>*</c> tenant's row from before the upgrade, or a trigger whose name became scheduled. It is unscheduled first,
    /// in the registered <c>*</c> tenant's scope where there is one, because that is where it was scheduled.
    /// </remarks>
    private async Task<IndexedWorkflowTriggers> ReconcileSharedTriggersAsync(Workflow workflow, ICollection<StoredTrigger> triggers, CancellationToken cancellationToken)
    {
        var currentTriggers = await FindSharedTriggersAsync(new() { WorkflowDefinitionId = workflow.Identity.DefinitionId }, true, cancellationToken);
        var diff = Diff.For(currentTriggers, triggers, _triggerEqualityComparer);
        var scheduledNames = ScheduledTriggerNames;
        var removedScheduledTriggers = diff.Removed.Where(x => IsScheduled(x, scheduledNames)).ToList();

        if (removedScheduledTriggers.Count > 0)
            await UnscheduleInAgnosticTenantAsync(workflow, removedScheduledTriggers, cancellationToken);

        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var removedSharedTriggers = diff.Removed.Except(removedScheduledTriggers).ToList();
        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, diff.Added, removedSharedTriggers, diff.Unchanged);
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
        return new(workflow, diff.Added, diff.Removed, diff.Unchanged);
    }

    /// <summary>
    /// Sends the removal of rows stamped <c>*</c> with a scheduled name, so that the trigger scheduler unschedules them, in the scope of a
    /// registered <c>*</c> tenant, or here when there is none.
    /// </summary>
    private async Task UnscheduleInAgnosticTenantAsync(Workflow workflow, List<StoredTrigger> removed, CancellationToken cancellationToken)
    {
        var notification = new WorkflowTriggersIndexed(new(workflow, [], removed, []));
        // The tenant service knows only the tenants activated so far; the registry also knows one that activates later.
        var agnosticTenant = _serviceProvider.GetService<ITenantService>() is { } tenantService ? await tenantService.FindAsync(Tenant.AgnosticTenantId, cancellationToken) : null;
        agnosticTenant ??= _serviceProvider.GetService<ITenantsProvider>() is { } tenantsProvider ? await tenantsProvider.FindByIdAsync(Tenant.AgnosticTenantId, cancellationToken) : null;
        var tenantScopeFactory = _serviceProvider.GetService<ITenantScopeFactory>();

        if (agnosticTenant == null || tenantScopeFactory == null || CurrentTenantId == Tenant.AgnosticTenantId)
        {
            await _notificationSender.SendAsync(notification, NotificationStrategy.Sequential, cancellationToken);
            return;
        }

        await using var scope = tenantScopeFactory.CreateScope(agnosticTenant);
        await scope.ServiceProvider.GetRequiredService<INotificationSender>().SendAsync(notification, NotificationStrategy.Sequential, cancellationToken);
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
            // A tenant whose database does not hold the version as published, such as one with its own database, could not start it, so it
            // gets no copies.
            var resolvable = scheduledTriggers.Count == 0 || await indexer.IsPublishedAsync(workflow.Identity.Id, cancellationToken);
            var result = await indexer.ReconcileTenantCopiesAsync(workflow, resolvable ? scheduledTriggers : [], false, cancellationToken);

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
    /// <param name="sharedWorkflow">The tenant-agnostic workflow.</param>
    /// <param name="scheduledTriggers">Its scheduled triggers.</param>
    /// <param name="replaceAll">Whether every existing copy is replaced, so that it is scheduled again.</param>
    /// <param name="comparer">Tells an unchanged copy from a changed one; by default, every value of the trigger counts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<IndexedWorkflowTriggers?> ReconcileTenantCopiesAsync(Workflow sharedWorkflow, IReadOnlyCollection<StoredTrigger> scheduledTriggers, bool replaceAll, CancellationToken cancellationToken, IEqualityComparer<StoredTrigger>? comparer = null)
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

        var diff = replaceAll ? Diff.From<StoredTrigger>(newTriggers, currentTriggers, []) : Diff.For(currentTriggers, newTriggers, comparer ?? _triggerEqualityComparer);

        // Removed copies are unscheduled before they are deleted, so a failure leaves them to be found again; added copies are scheduled
        // only once they are stored.
        if (diff.Removed.Count > 0)
            await _notificationSender.SendAsync(new WorkflowTriggersIndexed(new(sharedWorkflow, [], diff.Removed, [])), NotificationStrategy.Sequential, cancellationToken);

        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        if (diff.Added.Count > 0 || diff.Unchanged.Count > 0)
            await _notificationSender.SendAsync(new WorkflowTriggersIndexed(new(sharedWorkflow, diff.Added, [], diff.Unchanged)), cancellationToken);

        return new(sharedWorkflow, diff.Added, diff.Removed, diff.Unchanged);
    }

    /// <summary>
    /// Reconciles the current tenant's copies of the published tenant-agnostic definition with the specified ID, if there is one.
    /// The caller holds the indexing lock of the definition ID.
    /// </summary>
    private async Task ReconcileTenantCopiesOfPublishedSharedDefinitionAsync(string definitionId, bool replaceAll, CancellationToken cancellationToken)
    {
        if (CurrentTenantId == Tenant.AgnosticTenantId)
            return;

        var publishedVersions = await WorkflowDefinitionStore.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Published }, cancellationToken);
        var definition = publishedVersions.FirstOrDefault(x => x.TenantId == Tenant.AgnosticTenantId);

        if (definition == null)
            return;

        var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var scheduledNames = ScheduledTriggerNames;
        var scheduledTriggers = (await GetTriggersInternalAsync(workflowGraph.Workflow, cancellationToken).ToListAsync(cancellationToken))
            .Where(x => IsScheduled(x, scheduledNames))
            .ToList();
        // Deriving a timer again gives it a new start time. A copy of the same version and activity is the same trigger, so it is kept as it
        // is rather than replaced, which would schedule it again.
        await ReconcileTenantCopiesAsync(workflowGraph.Workflow, scheduledTriggers, replaceAll, cancellationToken, SameVersionAndActivityComparer.Instance);
    }

    /// <summary>
    /// Removes and unschedules the current tenant's scheduled copies of tenant-agnostic definition versions that are no longer published,
    /// for example because the definition was retracted or deleted while the tenant was inactive. The rows of the tenant's own definitions
    /// stay. A row whose version no longer exists is taken to be a copy: deleting a tenant's own version removes its rows in that tenant.
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
        var versions = (await WorkflowDefinitionStore.FindSummariesAsync(new WorkflowDefinitionFilter { Ids = versionIds }, cancellationToken)).ToDictionary(x => x.Id);
        var staleVersionIds = versionIds.Where(x => !versions.TryGetValue(x, out var version) || version is { TenantId: Tenant.AgnosticTenantId, IsPublished: false }).ToHashSet();

        foreach (var group in triggers.Where(x => staleVersionIds.Contains(x.WorkflowDefinitionVersionId)).GroupBy(x => x.WorkflowDefinitionVersionId))
        {
            var removed = group.ToList();

            // Classified without the lock: the version may have been published again meanwhile, which makes these rows current copies.
            await using (await AcquireDefinitionLockAsync(removed[0].WorkflowDefinitionId, cancellationToken))
            {
                var version = (await WorkflowDefinitionStore.FindSummariesAsync(new WorkflowDefinitionFilter { Id = group.Key }, cancellationToken)).FirstOrDefault();

                if (version != null && (version.TenantId != Tenant.AgnosticTenantId || version.IsPublished))
                    continue;

                var workflow = await FindWorkflowAsync(group.Key, removed[0].WorkflowDefinitionId, cancellationToken);

                // Unscheduled before the rows are deleted, so a failure leaves them to be found again by the next activation.
                await RemoveTriggersAsync(workflow, removed, true, cancellationToken);
            }
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
            result.AddRange(await FindSharedTriggersAsync(filter, false, cancellationToken));

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

        var versions = await WorkflowDefinitionStore.FindSummariesAsync(definitionFilter, cancellationToken);
        var sharedVersionIds = versions.Where(x => x.TenantId == Tenant.AgnosticTenantId).Select(x => x.Id).ToList();

        if (sharedVersionIds.Count == 0)
            return;

        // The copies that match the original filter, among the versions of tenant-agnostic definitions only.
        var copyFilter = filter.Clone();
        copyFilter.TenantAgnostic = false;
        copyFilter.WorkflowDefinitionVersionIds = filter.WorkflowDefinitionVersionIds?.Intersect(sharedVersionIds).ToList() ?? sharedVersionIds;

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
    /// Returns the rows stamped <c>*</c> that match the filter, with or without those whose name is scheduled. A tenant's lookups hide them
    /// while the tenant overrides their definition, so they are read with a tenant-agnostic filter, in the current tenant's database.
    /// </summary>
    private async Task<List<StoredTrigger>> FindSharedTriggersAsync(TriggerFilter filter, bool includeScheduled, CancellationToken cancellationToken)
    {
        var tenantAgnosticFilter = filter.Clone();
        tenantAgnosticFilter.TenantAgnostic = true;
        var triggers = await _triggerStore.FindManyAsync(tenantAgnosticFilter, cancellationToken);
        var scheduledNames = ScheduledTriggerNames;
        return triggers.Where(x => x.TenantId == Tenant.AgnosticTenantId && (includeScheduled || !IsScheduled(x, scheduledNames))).ToList();
    }

    /// <summary>
    /// Returns the versions of the current tenant's own definition with the specified ID, without those of the tenant-agnostic definition.
    /// </summary>
    private async Task<List<WorkflowDefinitionSummary>> FindOwnVersionsAsync(string definitionId, CancellationToken cancellationToken)
    {
        var versions = await WorkflowDefinitionStore.FindSummariesAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId }, cancellationToken);
        return versions.Where(x => x.TenantId != Tenant.AgnosticTenantId).ToList();
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
                var indexer = AsTriggerIndexer(scope.ServiceProvider.GetRequiredService<ITriggerIndexer>(), _logger) ?? ActivatorUtilities.CreateInstance<TriggerIndexer>(scope.ServiceProvider);
                await action(indexer, tenantId);
            }
            catch (Exception e) when (!throwOnFailure && e is not OperationCanceledException)
            {
                _logger.LogError(e, "Failed to reconcile the triggers of tenant-agnostic workflow definitions for tenant {TenantId}; its rows and schedules of them stay as they are until its next activation.", tenant.Id);
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

    /// <summary>
    /// Returns the trigger indexer as Elsa's <see cref="TriggerIndexer"/>, which reconciles the triggers of tenant-agnostic definitions on
    /// tenant activation, after deletions and across tenants. A host that registers another <see cref="ITriggerIndexer"/> does without
    /// that; this is logged once, as a warning, when a logger is given.
    /// </summary>
    internal static TriggerIndexer? AsTriggerIndexer(ITriggerIndexer triggerIndexer, ILogger? logger)
    {
        if (triggerIndexer is TriggerIndexer elsaTriggerIndexer)
            return elsaTriggerIndexer;

        if (logger != null && Interlocked.Exchange(ref _replacedIndexerLogged, 1) == 0)
            logger.LogWarning("The registered trigger indexer {TriggerIndexerType} is not Elsa's TriggerIndexer, so the triggers of tenant-agnostic workflow definitions are not reconciled on tenant activation, after deletions or for every tenant.", triggerIndexer.GetType().FullName);

        return null;
    }

    private static int _replacedIndexerLogged;

    /// <summary>
    /// Compares tenant copies by the definition version and activity they come from and their name, not by their payload.
    /// </summary>
    private sealed class SameVersionAndActivityComparer : IEqualityComparer<StoredTrigger>
    {
        public static readonly SameVersionAndActivityComparer Instance = new();

        public bool Equals(StoredTrigger? x, StoredTrigger? y) =>
            x?.WorkflowDefinitionVersionId == y?.WorkflowDefinitionVersionId && x?.ActivityId == y?.ActivityId && x?.Name == y?.Name;

        public int GetHashCode(StoredTrigger obj) => HashCode.Combine(obj.WorkflowDefinitionVersionId, obj.ActivityId, obj.Name);
    }

    private static bool IsScheduled(StoredTrigger trigger, ISet<string> scheduledNames) => trigger.Name != null && scheduledNames.Contains(trigger.Name);

    private static bool IsOwnedBy(StoredTrigger trigger, string tenantId) => trigger.TenantId.NormalizeTenantId() == tenantId;

    /// <summary>
    /// Returns whether the row belongs to the tenant rather than to all tenants. The rows stamped <c>*</c> of a registered <c>*</c> tenant
    /// are its scheduled rows; its unscheduled ones are the shared rows.
    /// </summary>
    private static bool IsTenantRow(StoredTrigger trigger, string tenantId, ISet<string> scheduledNames) =>
        IsOwnedBy(trigger, tenantId) && (tenantId != Tenant.AgnosticTenantId || IsScheduled(trigger, scheduledNames));
}
