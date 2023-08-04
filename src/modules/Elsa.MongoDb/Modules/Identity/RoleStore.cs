using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDb.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IRoleStore"/>.
/// </summary>
public class MongoRoleStore : IRoleStore
{
    private readonly MongoDbStore<Role> _roleMongoDbStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoRoleStore"/>.
    /// </summary>
    public MongoRoleStore(MongoDbStore<Role> roleMongoDbStore)
    {
        _roleMongoDbStore = roleMongoDbStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Role application, CancellationToken cancellationToken = default) => 
        await _roleMongoDbStore.SaveAsync(application, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Role role, CancellationToken cancellationToken = default) => 
        await _roleMongoDbStore.SaveAsync(role, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default) => 
        await _roleMongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);

    /// <inheritdoc />
    public async Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default) => 
        await _roleMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) => 
        await _roleMongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken);

    private static IMongoQueryable<Role> Filter(IQueryable<Role> query, RoleFilter filter) => (filter.Apply(query) as IMongoQueryable<Role>)!;
}