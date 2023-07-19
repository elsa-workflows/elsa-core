using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class MemoryTriggerStore : ITriggerStore
{
    private readonly MemoryStore<StoredTrigger> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryTriggerStore"/> class.
    /// </summary>
    public MemoryTriggerStore(MemoryStore<StoredTrigger> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return new();
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
        return new();
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(filter.Apply);
        return new(entities);
    }
    
    /// <inheritdoc />
    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(removed, x => x.Id);
        _store.SaveMany(added, x => x.Id);
        return new();
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.Id);
        return _store.DeleteMany(ids);
    }
}