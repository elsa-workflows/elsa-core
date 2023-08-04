using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDb.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// A MongoDB implementation of <see cref="IUserStore"/>.
/// </summary>
public class MongoUserStore : IUserStore
{
    private readonly MongoDbStore<User> _userMongoDbStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoUserStore"/>.
    /// </summary>
    public MongoUserStore(MongoDbStore<User> userMongoDbStore)
    {
        _userMongoDbStore = userMongoDbStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default) => 
        await _userMongoDbStore.SaveAsync(user, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => 
        await _userMongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => 
        await _userMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);

    private static IMongoQueryable<User> Filter(IQueryable<User> query, UserFilter filter) => (filter.Apply(query) as IMongoQueryable<User>)!;
}