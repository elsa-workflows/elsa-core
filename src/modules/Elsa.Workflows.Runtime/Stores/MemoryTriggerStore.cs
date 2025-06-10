using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Stores;

/// <inheritdoc />
[UsedImplicitly]
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
    public ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(filter.Apply).FirstOrDefault();
        return new(entity);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(filter.Apply);
        return new(entities);
    }

    public ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return FindManyAsync(filter, pageArgs, new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending), cancellationToken);
    }

    public ValueTask<Page<StoredTrigger>> FindManyAsync<TOrderBy>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(filter.Apply).LongCount();
        var result = _store.Query(query => filter.Apply(query).OrderBy(order).Paginate(pageArgs)).ToList();
        return ValueTask.FromResult(Page.Of(result, count));
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