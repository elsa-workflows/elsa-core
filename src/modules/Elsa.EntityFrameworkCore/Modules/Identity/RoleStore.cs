using System.Text.Json;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Identity;

/// <summary>
/// An EF Core implementation of <see cref="IRoleStore"/>.
/// </summary>
public class EFCoreRoleStore : IRoleStore
{
    private readonly EntityStore<IdentityElsaDbContext, Role> _applicationStore;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreRoleStore"/>.
    /// </summary>
    public EFCoreRoleStore(EntityStore<IdentityElsaDbContext, Role> applicationStore)
    {
        _applicationStore = applicationStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Role application, CancellationToken cancellationToken = default)
    {
        await _applicationStore.SaveAsync(application, SaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _applicationStore.AddAsync(role, SaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        await _applicationStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        return await _applicationStore.FindAsync(query => Filter(query, filter), LoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        return await _applicationStore.QueryAsync(queryable => Filter(queryable, filter), LoadAsync, cancellationToken).ToList();
    }

    private static ValueTask<Role> SaveAsync(IdentityElsaDbContext dbContext, Role role, CancellationToken cancellationToken)
    {
        var permissionsJson = JsonSerializer.Serialize(role.Permissions);
        dbContext.Entry(role).Property("Permissions").CurrentValue = permissionsJson;
        return new ValueTask<Role>(role);
    }

    private static ValueTask<Role?> LoadAsync(IdentityElsaDbContext dbContext, Role? role, CancellationToken cancellationToken)
    {
        if (role == null)
            return new(default(Role));

        var permissionsJson = (string?)dbContext.Entry(role).Property("Permissions").CurrentValue;

        if (string.IsNullOrWhiteSpace(permissionsJson)) 
            return new ValueTask<Role?>(role);
        
        role.Permissions = JsonSerializer.Deserialize<ICollection<string>>(permissionsJson)!;
        return new ValueTask<Role?>(role);
    }

    private static IQueryable<Role> Filter(IQueryable<Role> query, RoleFilter filter) => filter.Apply(query);
}