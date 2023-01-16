using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreTriggerStore : ITriggerStore
{
    private readonly Store<RuntimeElsaDbContext, StoredTrigger> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreTriggerStore(Store<RuntimeElsaDbContext, StoredTrigger> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindAsync(string hash, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => query.Where(x => x.Hash == hash), cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => query.Where(x => x.WorkflowDefinitionId == workflowDefinitionId), cancellationToken);

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        await DeleteManyAsync(removed.Select(r => r.Id), cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(query => query.Where(x => x.Name == activityType), cancellationToken);
}