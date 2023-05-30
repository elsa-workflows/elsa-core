using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDB.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IRoleStore"/>.
/// </summary>
public class MongoRoleStore : IRoleStore
{
    private readonly Store<Role> _roleStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoRoleStore"/>.
    /// </summary>
    public MongoRoleStore(Store<Role> roleStore)
    {
        _roleStore = roleStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Role application, CancellationToken cancellationToken = default) => 
        await _roleStore.SaveAsync(application, cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default) => 
        await _roleStore.SaveAsync(role, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default) => 
        await _roleStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default) => 
        await _roleStore.FindAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) => 
        await _roleStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken);

    private static IMongoQueryable<Role> Filter(IQueryable<Role> query, RoleFilter filter) => (filter.Apply(query) as IMongoQueryable<Role>)!;
}