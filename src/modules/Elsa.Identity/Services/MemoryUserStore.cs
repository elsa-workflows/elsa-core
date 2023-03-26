using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory user store.
/// </summary>
public class MemoryUserStore : IUserStore
{
    private readonly MemoryStore<User> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryUserStore"/> class.
    /// </summary>
    public MemoryUserStore(MemoryStore<User> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        _store.Save(user, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = _store.Query(query => Filter(query, filter)).Select(x => x.Id).Distinct().ToList();
        _store.DeleteWhere(x => ids.Contains(x.Id));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }
    
    private IQueryable<User> Filter(IQueryable<User> queryable, UserFilter filter) => filter.Apply(queryable);
}