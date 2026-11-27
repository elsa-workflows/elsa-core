using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Tenants.Options;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// A decorator for <see cref="ITriggerStore"/> that hides the shared triggers of a tenant-agnostic workflow definition from a tenant that
/// overrides it: a tenant overrides a tenant-agnostic definition while it has a published version of its own definition with the same
/// definition ID. A draft alone does not override.
/// </summary>
/// <remarks>
/// It applies to the lookups of a tenant when multitenancy is enabled, not to tenant-agnostic queries (<see cref="TriggerFilter.TenantAgnostic"/>)
/// or to a <c>*</c> context. It checks for overriding definitions only when a lookup returns shared triggers. It is registered beneath
/// <see cref="CachingTriggerStore"/>, so a cached lookup does not repeat the check.
/// </remarks>
internal class TenantOverrideTriggerStore(ITriggerStore decoratedStore, IServiceProvider serviceProvider) : ITriggerStore
{
    /// <inheritdoc />
    public ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => decoratedStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => decoratedStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var trigger = await decoratedStore.FindAsync(filter, cancellationToken);

        if (trigger == null || (await HideOverriddenAsync([trigger], filter, cancellationToken)).Count == 1)
            return trigger;

        return (await FindManyAsync(filter, cancellationToken)).FirstOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var triggers = await decoratedStore.FindManyAsync(filter, cancellationToken);
        return await HideOverriddenAsync(triggers.ToList(), filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var order = new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending);
        return await FindPageAsync(filter, pageArgs, order, () => decoratedStore.FindManyAsync(filter, pageArgs, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        return await FindPageAsync(filter, pageArgs, order, () => decoratedStore.FindManyAsync(filter, pageArgs, order, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default) => decoratedStore.ReplaceAsync(removed, added, cancellationToken);

    /// <inheritdoc />
    public ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => decoratedStore.DeleteManyAsync(filter, cancellationToken);

    /// <summary>
    /// Returns the requested page of the triggers the tenant sees. While no shared trigger of a definition the tenant overrides matches the
    /// lookup, that is the store's page; finding out takes one query for the matching rows of the overridden definitions only. Otherwise
    /// the store cannot leave out the hidden rows itself, so the lookup is filtered, counted and paged here; a page then never comes back
    /// short because of rows hidden on it, and its total count is exact.
    /// </summary>
    private async Task<Page<StoredTrigger>> FindPageAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, Func<ValueTask<Page<StoredTrigger>>> findStorePage, CancellationToken cancellationToken)
    {
        if (!AppliesTo(filter))
            return await findStorePage();

        var overriddenDefinitionIds = await FindOverriddenDefinitionIdsAsync(null, cancellationToken);

        if (overriddenDefinitionIds.Count == 0 || !await MatchesHiddenTriggerAsync(filter, overriddenDefinitionIds, cancellationToken))
            return await findStorePage();

        var triggers = Hide(await decoratedStore.FindManyAsync(filter, cancellationToken), overriddenDefinitionIds).AsQueryable();
        var items = triggers.OrderBy(order).Paginate(pageArgs).ToList();
        return Page.Of(items, triggers.LongCount());
    }

    private async Task<List<StoredTrigger>> HideOverriddenAsync(List<StoredTrigger> triggers, TriggerFilter filter, CancellationToken cancellationToken)
    {
        if (triggers.All(x => x.TenantId != Tenant.AgnosticTenantId) || !AppliesTo(filter))
            return triggers;

        var sharedDefinitionIds = triggers.Where(x => x.TenantId == Tenant.AgnosticTenantId).Select(x => x.WorkflowDefinitionId).Distinct().ToList();
        var overriddenDefinitionIds = await FindOverriddenDefinitionIdsAsync(sharedDefinitionIds, cancellationToken);
        return overriddenDefinitionIds.Count == 0 ? triggers : Hide(triggers, overriddenDefinitionIds).ToList();
    }

    private async Task<bool> MatchesHiddenTriggerAsync(TriggerFilter filter, IReadOnlySet<string> overriddenDefinitionIds, CancellationToken cancellationToken)
    {
        var definitionIds = overriddenDefinitionIds
            .Where(x => filter.WorkflowDefinitionId == null || filter.WorkflowDefinitionId == x)
            .Where(x => filter.WorkflowDefinitionIds == null || filter.WorkflowDefinitionIds.Contains(x))
            .ToList();

        if (definitionIds.Count == 0)
            return false;

        // Unpaged, but bounded: only the rows of the definitions the tenant overrides that match the lookup.
        var overriddenFilter = filter.Clone();
        overriddenFilter.WorkflowDefinitionIds = definitionIds;
        var triggers = await decoratedStore.FindManyAsync(overriddenFilter, cancellationToken);
        return triggers.Any(x => x.TenantId == Tenant.AgnosticTenantId);
    }

    private bool AppliesTo(TriggerFilter filter) =>
        !filter.TenantAgnostic
        && serviceProvider.GetService<IOptions<TenantsOptions>>()?.Value.IsEnabled == true
        && serviceProvider.GetRequiredService<ITenantAccessor>().TenantId != Tenant.AgnosticTenantId;

    /// <summary>
    /// Returns the IDs of the tenant-agnostic definitions, among <paramref name="definitionIds"/> or all, of which the tenant has a published
    /// definition of its own. The tenant sees its own definitions and the tenant-agnostic ones.
    /// </summary>
    private async Task<HashSet<string>> FindOverriddenDefinitionIdsAsync(ICollection<string>? definitionIds, CancellationToken cancellationToken)
    {
        var definitionFilter = new WorkflowDefinitionFilter { DefinitionIds = definitionIds, VersionOptions = VersionOptions.Published };
        var publishedVersions = (await serviceProvider.GetRequiredService<IWorkflowDefinitionStore>().FindSummariesAsync(definitionFilter, cancellationToken)).ToList();
        var sharedDefinitionIds = publishedVersions.Where(x => x.TenantId == Tenant.AgnosticTenantId).Select(x => x.DefinitionId).ToHashSet();
        return publishedVersions.Where(x => x.TenantId != Tenant.AgnosticTenantId && sharedDefinitionIds.Contains(x.DefinitionId)).Select(x => x.DefinitionId).ToHashSet();
    }

    private static IEnumerable<StoredTrigger> Hide(IEnumerable<StoredTrigger> triggers, IReadOnlySet<string> overriddenDefinitionIds) =>
        triggers.Where(x => x.TenantId != Tenant.AgnosticTenantId || !overriddenDefinitionIds.Contains(x.WorkflowDefinitionId));
}
