using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Implementations;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class MemoryTriggerStore : ITriggerStore
{
    private readonly MemoryStore<StoredTrigger> _store;

    public MemoryTriggerStore(MemoryStore<StoredTrigger> store)
    {
        _store = store;
    }

    public Task SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<StoredTrigger>> FindAsync(string hash, CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query => query.Where(x => x.Hash == hash));
        return Task.FromResult<IEnumerable<StoredTrigger>>(triggers.ToList());
    }

    public Task<IEnumerable<StoredTrigger>> FindManyByWorkflowDefinitionIdAsync(
        string workflowDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query => query.Where(x => x.WorkflowDefinitionId == workflowDefinitionId));
        return Task.FromResult<IEnumerable<StoredTrigger>>(triggers.ToList());
    }

    public Task ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added,
        CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(removed, x => x.Id);
        _store.SaveMany(added, x => x.Id);

        return Task.CompletedTask;
    }

    public Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(ids);
        return Task.CompletedTask;
    }
}