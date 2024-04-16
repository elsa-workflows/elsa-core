using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDb.Common;
using JetBrains.Annotations;

namespace Elsa.MongoDb.Modules.Labels;

/// <inheritdoc />
[UsedImplicitly]
public class MongoWorkflowDefinitionLabelStore(MongoDbStore<WorkflowDefinitionLabel> mongoDbStore) : IWorkflowDefinitionLabelStore
{
    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await mongoDbStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.FindManyAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        var idList = removed.Select(r => r.Id);
        await mongoDbStore.DeleteWhereAsync(w => idList.Contains(w.Id), cancellationToken);
        await mongoDbStore.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        return mongoDbStore.DeleteWhereAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        return mongoDbStore.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        return mongoDbStore.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionVersionId), cancellationToken);
    }
}