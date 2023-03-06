using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreTriggerStore : ITriggerStore
{
    private readonly EntityStore<RuntimeElsaDbContext, StoredTrigger> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreTriggerStore(EntityStore<RuntimeElsaDbContext, StoredTrigger> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, cancellationToken);

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var filter = new TriggerFilter { Ids = removed.Select(r => r.Id).ToList() };
        await DeleteManyAsync(filter, cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
}