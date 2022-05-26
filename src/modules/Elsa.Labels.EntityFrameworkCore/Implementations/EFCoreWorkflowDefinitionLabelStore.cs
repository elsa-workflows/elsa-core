using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;

namespace Elsa.Labels.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly IStore<LabelsDbContext, WorkflowDefinitionLabel> _store;
    public EFCoreWorkflowDefinitionLabelStore(IStore<LabelsDbContext, WorkflowDefinitionLabel> store) => _store = store;

    public async Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    public async Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId, cancellationToken);

    public async Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        await _store.DeleteManyAsync(removed, cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }
}