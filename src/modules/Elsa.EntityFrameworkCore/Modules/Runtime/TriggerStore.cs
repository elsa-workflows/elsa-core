using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

public class EFCoreTriggerStore : ITriggerStore
{
    private readonly Store<RuntimeElsaDbContext, StoredTrigger> _store;

    public EFCoreTriggerStore(Store<RuntimeElsaDbContext, StoredTrigger> store)
    {
        _store = store;
    }

    public async Task SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    public async Task<IEnumerable<StoredTrigger>> FindAsync(
        string hash, 
        CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => query.Where(x => x.Hash == hash), cancellationToken);

    public async Task<IEnumerable<StoredTrigger>> FindManyByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => query.Where(x => x.WorkflowDefinitionId == workflowDefinitionId), cancellationToken);

    public async Task ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        await _store.DeleteManyAsync(removed, cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }

    public async Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }
}