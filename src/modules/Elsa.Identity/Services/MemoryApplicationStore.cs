using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory application store.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Application contracts have no TenantAgnostic flag, so isolation always applies (EF query filter).
/// </remarks>
public class MemoryApplicationStore : IApplicationStore
{
    private readonly MemoryStore<Application> _store;
    private readonly ITenantAccessor _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryApplicationStore"/> class.
    /// </summary>
    public MemoryApplicationStore(MemoryStore<Application> store, ITenantAccessor tenantAccessor)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(Application application, CancellationToken cancellationToken = default)
    {
        ApplyCurrentTenant(application);
        lock (_store.Sync)
            _store.Save(application, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.DeleteWhere(application =>
                IsVisible(application) && filter.Apply(new[] { application }.AsQueryable()).Any());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    private IQueryable<Application> Filter(IQueryable<Application> queryable, ApplicationFilter filter) =>
        filter.Apply(queryable.WhereVisibleToTenant(_tenantAccessor.TenantId));

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, _tenantAccessor.TenantId);

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
