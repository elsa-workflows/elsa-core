using Elsa.Common.Implementations;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;

namespace Elsa.Labels.Implementations;

public class InMemoryWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly MemoryStore<WorkflowDefinitionLabel> _store;

    public InMemoryWorkflowDefinitionLabelStore(MemoryStore<WorkflowDefinitionLabel> store)
    {
        _store = store;
    }

    public Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
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
        _store.DeleteMany(removed, x => x.Id);
        _store.SaveMany(added, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<int> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteWhere(x => x.WorkflowDefinitionId == workflowDefinitionId);
        return Task.FromResult(result);
    }

    public Task<int> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteWhere(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId);
        return Task.FromResult(result);
    }

    public Task<int> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        var result = _store.DeleteWhere(x => ids.Contains(x.WorkflowDefinitionId));
        return Task.FromResult(result);
    }

    public Task<int> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        var result = _store.DeleteWhere(x => ids.Contains(x.WorkflowDefinitionVersionId));
        return Task.FromResult(result);
    }
}