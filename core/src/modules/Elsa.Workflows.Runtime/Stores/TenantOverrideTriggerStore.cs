using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
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
    private static readonly Tenant SharedView = new() { Id = Tenant.AgnosticTenantId, Name = Tenant.AgnosticTenantId };

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
        var page = await decoratedStore.FindManyAsync(filter, pageArgs, cancellationToken);
        return await HideOverriddenAsync(page, filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        var page = await decoratedStore.FindManyAsync(filter, pageArgs, order, cancellationToken);
        return await HideOverriddenAsync(page, filter, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default) => decoratedStore.ReplaceAsync(removed, added, cancellationToken);

    /// <inheritdoc />
    public ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => decoratedStore.DeleteManyAsync(filter, cancellationToken);

    /// <remarks>
    /// The total count is reduced by the hidden triggers of this page only, so it can overstate the count of a lookup that spans several pages.
    /// </remarks>
    private async Task<Page<StoredTrigger>> HideOverriddenAsync(Page<StoredTrigger> page, TriggerFilter filter, CancellationToken cancellationToken)
    {
        var items = await HideOverriddenAsync(page.Items.ToList(), filter, cancellationToken);
        return items.Count == page.Items.Count ? page : Page.Of(items, page.TotalCount - (page.Items.Count - items.Count));
    }

    private async Task<List<StoredTrigger>> HideOverriddenAsync(List<StoredTrigger> triggers, TriggerFilter filter, CancellationToken cancellationToken)
    {
        if (filter.TenantAgnostic || triggers.All(x => x.TenantId != Tenant.AgnosticTenantId))
            return triggers;

        if (serviceProvider.GetService<IOptions<TenantsOptions>>()?.Value.IsEnabled != true)
            return triggers;

        var tenantAccessor = serviceProvider.GetRequiredService<ITenantAccessor>();
        var tenantId = tenantAccessor.TenantId;

        if (tenantId == Tenant.AgnosticTenantId)
            return triggers;

        // The published versions the tenant sees, less the tenant-agnostic ones, are its own. Summaries carry no tenant, so the
        // tenant-agnostic ones are read in a "*" context, where only they are visible.
        var sharedDefinitionIds = triggers.Where(x => x.TenantId == Tenant.AgnosticTenantId).Select(x => x.WorkflowDefinitionId).Distinct().ToList();
        var definitionFilter = new WorkflowDefinitionFilter { DefinitionIds = sharedDefinitionIds, VersionOptions = VersionOptions.Published };
        var definitionStore = serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var visibleVersions = await definitionStore.FindSummariesAsync(definitionFilter, cancellationToken);
        IEnumerable<WorkflowDefinitionSummary> sharedVersions;

        using (tenantAccessor.PushContext(SharedView))
            sharedVersions = await definitionStore.FindSummariesAsync(definitionFilter, cancellationToken);

        var sharedVersionIds = sharedVersions.Select(x => x.Id).ToHashSet();
        var overriddenDefinitionIds = visibleVersions.Where(x => !sharedVersionIds.Contains(x.Id)).Select(x => x.DefinitionId).ToHashSet();

        if (overriddenDefinitionIds.Count == 0)
            return triggers;

        return triggers.Where(x => x.TenantId != Tenant.AgnosticTenantId || !overriddenDefinitionIds.Contains(x.WorkflowDefinitionId)).ToList();
    }
}
