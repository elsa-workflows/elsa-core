using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;

namespace Elsa.Alterations.Core.Stores;

/// <summary>
/// A memory-based store for alteration plans.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Alterations contracts have no TenantAgnostic flag, so isolation always applies (EF query filter).
/// </remarks>
public class MemoryAlterationPlanStore : IAlterationPlanStore
{
    private readonly MemoryStore<AlterationPlan> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAlterationPlanStore"/> class.
    /// </summary>
    public MemoryAlterationPlanStore(MemoryStore<AlterationPlan> store, ITenantAccessor? tenantAccessor = null)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            ApplyCurrentTenant(plan);
            EnsureIdAvailable(plan);
            _store.Save(plan, x => x.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return Task.FromResult(count);
    }

    /// <remarks>
    /// Ambient tenant is applied here rather than in <see cref="AlterationPlanFilter.Apply"/>.
    /// EF owns that via <c>SetTenantIdFilter</c>; Memory must compensate.
    /// </remarks>
    private IQueryable<AlterationPlan> Filter(IQueryable<AlterationPlan> query, AlterationPlanFilter filter) =>
        filter.Apply(query.WhereVisibleToTenant(CurrentTenantId));

    private string CurrentTenantId => _tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, CurrentTenantId);

    private void EnsureIdAvailable(AlterationPlan plan)
    {
        var existing = _store.Find(x => x.Id == plan.Id);

        if (existing is not null && !IsVisible(existing))
        {
            throw new InvalidOperationException(
                $"An alteration plan with ID '{plan.Id}' already exists and is not visible to the current tenant.");
        }
    }

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
