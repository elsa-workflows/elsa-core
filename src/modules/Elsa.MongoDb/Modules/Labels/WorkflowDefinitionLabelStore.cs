using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDb.Common;

namespace Elsa.MongoDb.Modules.Labels;

/// <inheritdoc />
public class MongoWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly MongoDbStore<WorkflowDefinitionLabel> _mongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoWorkflowDefinitionLabelStore"/> class.
    /// </summary>
    public MongoWorkflowDefinitionLabelStore(MongoDbStore<WorkflowDefinitionLabel> mongoDbStore) => _mongoDbStore = mongoDbStore;

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _mongoDbStore.FindManyAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    /// <inheritdoc />
    public async Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        var idList = removed.Select(r => r.Id);
        await _mongoDbStore.DeleteWhereAsync(w => idList.Contains(w.Id), cancellationToken);
        await _mongoDbStore.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
        await _mongoDbStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _mongoDbStore.DeleteWhereAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        return await _mongoDbStore.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        return await _mongoDbStore.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionVersionId), cancellationToken);
    }
}