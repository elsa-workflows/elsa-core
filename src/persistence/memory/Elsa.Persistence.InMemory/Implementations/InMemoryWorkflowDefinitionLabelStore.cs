using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.InMemory.Implementations;

public class InMemoryWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly InMemoryStore<WorkflowDefinitionLabel> _store;

    public InMemoryWorkflowDefinitionLabelStore(InMemoryStore<WorkflowDefinitionLabel> store)
    {
        _store = store;
    }

    public Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default)
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

    public Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var result = _store.FindMany(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId);
        return Task.FromResult(result);
    }

    public Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(removed);
        _store.SaveMany(added);
        return Task.CompletedTask;
    }
}