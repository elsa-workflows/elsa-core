using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory role store.
/// </summary>
public class MemoryRoleStore : IRoleStore
{
    private readonly MemoryStore<Role> _store;
    private readonly ITenantAccessor _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRoleStore"/> class.
    /// </summary>
    public MemoryRoleStore(MemoryStore<Role> store, ITenantAccessor tenantAccessor)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store.Save(role, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = _store.Query(query => Filter(query, filter)).Select(x => x.Id).Distinct().ToList();
        _store.DeleteWhere(x => ids.Contains(x.Id));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store.Save(role, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }
    
    /// <remarks>
    /// The ambient tenant is applied here rather than left to callers. Isolation previously existed only
    /// on the Entity Framework path, and only when multitenancy was enabled, so a deployment running the
    /// default in-memory stores had none at all.
    /// </remarks>
    private IQueryable<Role> Filter(IQueryable<Role> queryable, RoleFilter filter)
    {
        var tenantId = _tenantAccessor.TenantId;
        queryable = queryable.Where(x => x.TenantId == tenantId || x.TenantId == Tenant.AgnosticTenantId || x.TenantId == null);

        return filter.Apply(queryable);
    }
}