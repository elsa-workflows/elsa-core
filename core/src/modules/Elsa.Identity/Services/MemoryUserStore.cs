using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory user store.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Null tenant IDs are visible only to the default tenant.
/// </remarks>
public class MemoryUserStore : IUserStore
{
    private readonly MemoryStore<User> _store;
    private readonly ITenantAccessor _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryUserStore"/> class.
    /// </summary>
    public MemoryUserStore(MemoryStore<User> store, ITenantAccessor tenantAccessor)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            ApplyCurrentTenant(user);
            MemoryIdentityUniqueness.EnsureAvailable(_store, user, x => x.Name, "name");
            _store.Save(user, x => x.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            _store.DeleteWhere(user =>
                IsVisible(user) && filter.Apply(new[] { user }.AsQueryable()).Any());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).Select(Clone).ToList();
        return Task.FromResult<IEnumerable<User>>(result);
    }

    /// <inheritdoc />
    public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).Select(Clone).FirstOrDefault();
        return Task.FromResult(result);
    }

    private IQueryable<User> Filter(IQueryable<User> queryable, UserFilter filter) =>
        filter.Apply(queryable.WhereVisibleToTenant(_tenantAccessor.TenantId));

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, _tenantAccessor.TenantId);

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }

    private static User Clone(User user) =>
        new()
        {
            Id = user.Id,
            Name = user.Name,
            TenantId = user.TenantId,
            HashedPassword = user.HashedPassword,
            HashedPasswordSalt = user.HashedPasswordSalt,
            Roles = user.Roles.ToList()
        };
}
