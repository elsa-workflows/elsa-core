using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory role store.
/// </summary>
public class MemoryRoleStore : IRoleStore, IRoleStoreWithAtomicDelete
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
        _store.Save(role, GetStorageKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var roles = _store.Query(query => Filter(query, filter)).ToList();
        _store.DeleteMany(roles, GetStorageKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The matching role's key is removed through the underlying concurrent dictionary, whose removal is a single
    /// compare-and-remove step. Two callers racing on the same role ID therefore see one <see langword="true"/> and
    /// one <see langword="false"/>, rather than both concluding they deleted it.
    /// </remarks>
    public Task<bool> TryDeleteAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = _store.Query(query => Filter(query, new RoleFilter { Id = roleId })).FirstOrDefault();
        var deleted = role is not null && _store.Delete(GetStorageKey(role));

        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public Task SaveAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store.Save(role, GetStorageKey);
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
    /// default in-memory stores had none at all. Null tenant IDs are retained only for the default tenant
    /// for backwards compatibility with records created before tenant assignment was added.
    /// </remarks>
    private IQueryable<Role> Filter(IQueryable<Role> queryable, RoleFilter filter)
    {
        var tenantId = _tenantAccessor.TenantId;
        queryable = queryable.Where(x => x.TenantId == tenantId || x.TenantId == Tenant.AgnosticTenantId || (x.TenantId == null && tenantId == Tenant.DefaultTenantId));

        return filter.Apply(queryable);
    }

    private static string GetStorageKey(Role role) => GetStorageKey(role.TenantId, role.Id);

    private static string GetStorageKey(string? tenantId, string roleId) =>
        $"{tenantId?.Length ?? -1}:{tenantId}{roleId.Length}:{roleId}";
}
