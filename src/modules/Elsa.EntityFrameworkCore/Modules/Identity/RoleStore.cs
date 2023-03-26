using System.Text.Json;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
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
        await _applicationStore.SaveAsync(application, Save, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _applicationStore.AddAsync(role, Save, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        await _applicationStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        return await _applicationStore.FindAsync(query => Filter(query, filter), Load, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        return await _applicationStore.QueryAsync(queryable => Filter(queryable, filter), Load, cancellationToken).ToList();
    }

    private static Role Save(IdentityElsaDbContext dbContext, Role role)
    {
        var permissionsJson = JsonSerializer.Serialize(role.Permissions);
        dbContext.Entry(role).Property("Permissions").CurrentValue = permissionsJson;
        return role;
    }

    private static Role? Load(IdentityElsaDbContext dbContext, Role? role)
    {
        if (role == null)
            return null;

        var permissionsJson = (string?)dbContext.Entry(role).Property("Permissions").CurrentValue;

        if (string.IsNullOrWhiteSpace(permissionsJson)) 
            return role;
        
        role.Permissions = JsonSerializer.Deserialize<ICollection<string>>(permissionsJson)!;
        return role;
    }

    private static IQueryable<Role> Filter(IQueryable<Role> query, RoleFilter filter) => filter.Apply(query);
}