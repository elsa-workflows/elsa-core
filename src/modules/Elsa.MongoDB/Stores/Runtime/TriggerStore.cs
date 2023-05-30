using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Runtime;

/// <inheritdoc />
public class MongoTriggerStore : ITriggerStore
{
    private readonly Store<StoredTrigger> _store;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoTriggerStore(Store<StoredTrigger> store, IPayloadSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => 
        await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => 
        await _store.FindManyAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var filter = new TriggerFilter { Ids = removed.Select(r => r.Id).ToList() };
        await DeleteManyAsync(filter, cancellationToken);
        await _store.SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    
    private static IMongoQueryable<StoredTrigger> Filter(IMongoQueryable<StoredTrigger> queryable, TriggerFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<StoredTrigger>)!;
}