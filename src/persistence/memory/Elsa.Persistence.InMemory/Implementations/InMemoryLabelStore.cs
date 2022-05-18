using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.InMemory.Implementations;

public class InMemoryLabelStore : ILabelStore
{
    private readonly InMemoryStore<Label> _store;

    public InMemoryLabelStore(InMemoryStore<Label> store)
    {
        _store = store;
    }

    public Task SaveAsync(Label record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = _store.Delete(id);
        return Task.FromResult(result);
    }

    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteMany(ids);
        return Task.FromResult(result);
    }

    public Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = _store.Find(x => x.Id == id);
        return Task.FromResult(record);
    }

    public Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable().OrderBy(x => x.Name);
        return query.PaginateAsync(pageArgs);
    }
}