using Elsa.Common.Services;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Services;

/// <summary>
/// An in-memory store of workflow-label associations.
/// </summary>
public class InMemoryWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly MemoryStore<WorkflowDefinitionLabel> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryWorkflowDefinitionLabelStore(MemoryStore<WorkflowDefinitionLabel> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = _store.Delete(id);
        return Task.FromResult(result);
    }
    
    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var result = _store.FindMany(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(removed, x => x.Id);
        _store.SaveMany(added, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteWhere(x => x.WorkflowDefinitionId == workflowDefinitionId);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteWhere(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        var result = _store.DeleteWhere(x => ids.Contains(x.WorkflowDefinitionId));
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        var result = _store.DeleteWhere(x => ids.Contains(x.WorkflowDefinitionVersionId));
        return Task.FromResult(result);
    }
    
    private Task<long> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteMany(ids);
        return Task.FromResult(result);
    }

}