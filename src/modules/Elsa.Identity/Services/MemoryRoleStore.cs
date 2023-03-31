using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory role store.
/// </summary>
public class MemoryRoleStore : IRoleStore
{
    private readonly MemoryStore<Role> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRoleStore"/> class.
    /// </summary>
    public MemoryRoleStore(MemoryStore<Role> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store.Save(role, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = _store.Query(query => Filter(query, filter)).Select(x => x.Id).Distinct().ToList();
        _store.DeleteWhere(x => ids.Contains(x.Id));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store.Save(role, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }
    
    private IQueryable<Role> Filter(IQueryable<Role> queryable, RoleFilter filter) => filter.Apply(queryable);
}