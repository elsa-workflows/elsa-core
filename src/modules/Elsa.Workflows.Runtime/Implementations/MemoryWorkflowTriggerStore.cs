using Elsa.Common.Implementations;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class MemoryWorkflowTriggerStore : IWorkflowTriggerStore
{
    private readonly MemoryStore<WorkflowTrigger> _store;

    public MemoryWorkflowTriggerStore(MemoryStore<WorkflowTrigger> store)
    {
        _store = store;
    }

    public Task SaveAsync(WorkflowTrigger record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowTrigger> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<WorkflowTrigger>> FindManyByNameAsync(string name, string? hash, CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query =>
        {
            query = query.Where(x => x.Name == name);

            if (hash != null)
                query = query.Where(x => x.Hash == hash);

            return query;
        });

        return Task.FromResult<IEnumerable<WorkflowTrigger>>(triggers.ToList());
    }

    public Task<IEnumerable<WorkflowTrigger>> FindManyByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query => query.Where(x => x.WorkflowDefinitionId == workflowDefinitionId));
        return Task.FromResult<IEnumerable<WorkflowTrigger>>(triggers.ToList());
    }

    public Task ReplaceAsync(IEnumerable<WorkflowTrigger> removed, IEnumerable<WorkflowTrigger> added, CancellationToken cancellationToken = default)
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