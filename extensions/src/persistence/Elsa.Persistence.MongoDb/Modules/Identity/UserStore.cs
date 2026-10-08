using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.MongoDb.Common;
using JetBrains.Annotations;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Modules.Identity;

/// <summary>
/// A MongoDB implementation of <see cref="IUserStore"/>.
/// </summary>
[UsedImplicitly]
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

    /// <inheritdoc />
    public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        return userMongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken);
    }

    private static IQueryable<User> Filter(IQueryable<User> query, UserFilter filter)
    {
        return filter.Apply(query);
    }
}