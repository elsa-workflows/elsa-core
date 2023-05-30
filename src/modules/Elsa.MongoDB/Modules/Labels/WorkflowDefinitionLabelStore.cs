using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDB.Common;

namespace Elsa.MongoDB.Modules.Labels;

/// <inheritdoc />
public class MongoWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly MongoStore<WorkflowDefinitionLabel> _mongoStore;

    /// <summary>
    /// Constructor
    /// </summary>
    public MongoWorkflowDefinitionLabelStore(MongoStore<WorkflowDefinitionLabel> mongoStore) => _mongoStore = mongoStore;

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => 
        await _mongoStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _mongoStore.FindManyAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    /// <inheritdoc />
    public async Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        var idList = removed.Select(r => r.Id);
        await _mongoStore.DeleteWhereAsync(w => idList.Contains(w.Id), cancellationToken);
        await _mongoStore.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
        await _mongoStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _mongoStore.DeleteWhereAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        return await _mongoStore.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        return await _mongoStore.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionVersionId), cancellationToken);
    }
}