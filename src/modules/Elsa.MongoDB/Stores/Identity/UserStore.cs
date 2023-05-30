using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.MongoDB.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Identity;

/// <summary>
/// A MongoDb implementation of <see cref="IUserStore"/>.
/// </summary>
public class MongoUserStore : IUserStore
{
    private readonly Store<User> _userStore;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoUserStore"/>.
    /// </summary>
    public MongoUserStore(Store<User> userStore)
    {
        _userStore = userStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default) => 
        await _userStore.SaveAsync(user, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => 
        await _userStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => 
        await _userStore.FindAsync(query => Filter(query, filter), cancellationToken);

    private static IMongoQueryable<User> Filter(IQueryable<User> query, UserFilter filter) => (filter.Apply(query) as IMongoQueryable<User>)!;
}