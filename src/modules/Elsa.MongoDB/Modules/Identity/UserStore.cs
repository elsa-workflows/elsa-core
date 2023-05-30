using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDB.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Modules.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IUserStore"/>.
/// </summary>
public class MongoUserStore : IUserStore
{
    private readonly MongoStore<User> _userMongoStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoUserStore"/>.
    /// </summary>
    public MongoUserStore(MongoStore<User> userMongoStore)
    {
        _userMongoStore = userMongoStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default) => 
        await _userMongoStore.SaveAsync(user, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => 
        await _userMongoStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => 
        await _userMongoStore.FindAsync(query => Filter(query, filter), cancellationToken);

    private static IMongoQueryable<User> Filter(IQueryable<User> query, UserFilter filter) => (filter.Apply(query) as IMongoQueryable<User>)!;
}