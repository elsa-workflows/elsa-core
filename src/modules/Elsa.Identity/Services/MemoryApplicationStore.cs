using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <summary>
/// Represents an in-memory application store.
/// </summary>
public class MemoryApplicationStore : IApplicationStore
{
    private readonly MemoryStore<Application> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryApplicationStore"/> class.
    /// </summary>
    public MemoryApplicationStore(MemoryStore<Application> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task SaveAsync(Application application, CancellationToken cancellationToken = default)
    {
        _store.Save(application, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = _store.Query(query => Filter(query, filter)).Select(x => x.Id).Distinct().ToList();
        _store.DeleteWhere(x => ids.Contains(x.Id));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }
    
    private IQueryable<Application> Filter(IQueryable<Application> queryable, ApplicationFilter filter) => filter.Apply(queryable);
}