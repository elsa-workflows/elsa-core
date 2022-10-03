using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

public class EfCoreTriggerStore : ITriggerStore
{
    private readonly Store<RuntimeDbContext, StoredTrigger> _store;

    public EfCoreTriggerStore(Store<RuntimeDbContext, StoredTrigger> store)
    {
        _store = store;
    }

    public async Task SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    public async Task<IEnumerable<StoredTrigger>> FindAsync(string name, string? hash, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query =>
        {
            query = query.Where(x => x.Name == name);

            if (hash != null)
                query = query.Where(x => x.Hash == hash);

            return query;
        }, cancellationToken);

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