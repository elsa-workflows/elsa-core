using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;

namespace Elsa.Alterations.Core.Stores;

/// <summary>
/// A memory-based store for alteration jobs.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Alterations contracts have no TenantAgnostic flag, so isolation always applies (EF query filter).
/// </remarks>
public class MemoryAlterationJobStore : IAlterationJobStore
{
    private readonly MemoryStore<AlterationJob> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAlterationJobStore"/> class.
    /// </summary>
    public MemoryAlterationJobStore(MemoryStore<AlterationJob> store, ITenantAccessor? tenantAccessor = null)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(AlterationJob job, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            ApplyCurrentTenant(job);
            EnsureIdAvailable(job);
            _store.Save(job, x => x.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default)
    {
        var list = jobs.ToList();

        lock (_store.Sync)
        {
            foreach (var job in list)
                ApplyCurrentTenant(job);

            foreach (var job in list)
                EnsureIdAvailable(job);

            _store.SaveMany(list, x => x.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AlterationJob?> FindAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AlterationJob>> FindManyAsync(AlterationJobFilter filter, CancellationToken cancellationToken)
    {
        var entities = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> FindManyIdsAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = _store.Query(query => Filter(query, filter)).Select(x => x.Id).ToList().AsEnumerable();
        return Task.FromResult(ids);
    }

    /// <inheritdoc />
    public Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return Task.FromResult(count);
    }

    /// <remarks>
    /// Ambient tenant is applied here rather than in <see cref="AlterationJobFilter.Apply"/>.
    /// EF owns that via <c>SetTenantIdFilter</c>; Memory must compensate.
    /// </remarks>
    private IQueryable<AlterationJob> Filter(IQueryable<AlterationJob> query, AlterationJobFilter filter) =>
        filter.Apply(query.WhereVisibleToTenant(CurrentTenantId));

    private string CurrentTenantId => _tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, CurrentTenantId);

    private void EnsureIdAvailable(AlterationJob job)
    {
        var existing = _store.Find(x => x.Id == job.Id);

        if (existing is not null && !CanReplace(existing))
        {
            throw new InvalidOperationException(
                $"An alteration job with ID '{job.Id}' already exists and is not visible to the current tenant.");
        }
    }

    /// <summary>
    /// <c>*</c> is visible to every tenant, but only an agnostic writer may replace it.
    /// Named tenants may upsert their own visible rows.
    /// </summary>
    private bool CanReplace(Entity existing) =>
        existing.TenantId == Tenant.AgnosticTenantId
            ? CurrentTenantId == Tenant.AgnosticTenantId
            : IsVisible(existing);

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
