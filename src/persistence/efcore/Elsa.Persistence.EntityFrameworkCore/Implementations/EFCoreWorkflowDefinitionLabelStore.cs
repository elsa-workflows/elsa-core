using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    private readonly IStore<WorkflowDefinitionLabel> _store;
    public EFCoreWorkflowDefinitionLabelStore(IStore<WorkflowDefinitionLabel> store) => _store = store;

    public async Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }
}