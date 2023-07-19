using Elsa.EntityFrameworkCore.Common;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

/// <inheritdoc />
public class EFCoreWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly EntityStore<LabelsElsaDbContext, WorkflowDefinitionLabel> _store;

    /// <summary>
    /// Constructor
    /// </summary>
    public EFCoreWorkflowDefinitionLabelStore(EntityStore<LabelsElsaDbContext, WorkflowDefinitionLabel> store) => _store = store;

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    /// <inheritdoc />
    public async Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        var idList = removed.Select(r => r.Id);
        await _store.DeleteWhereAsync(w => idList.Contains(w.Id), cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        return await _store.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        return await _store.DeleteWhereAsync(x => ids.Contains(x.WorkflowDefinitionVersionId), cancellationToken);
    }
}