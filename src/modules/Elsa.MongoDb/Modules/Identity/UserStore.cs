using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDb.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// A MongoDB implementation of <see cref="IUserStore"/>.
/// </summary>
public class MongoUserStore(MongoDbStore<User> userMongoDbStore) : IUserStore
{
    /// <inheritdoc />
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        return userMongoDbStore.SaveAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        return userMongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        return userMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    private static IMongoQueryable<User> Filter(IQueryable<User> query, UserFilter filter)
    {
        return (filter.Apply(query) as IMongoQueryable<User>)!;
    }
}