using Elsa.MongoDB.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Modules.Runtime;

/// <inheritdoc />
public class MongoTriggerStore : ITriggerStore
{
    private readonly MongoStore<StoredTrigger> _mongoStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoTriggerStore(MongoStore<StoredTrigger> mongoStore)
    {
        _mongoStore = mongoStore;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoStore.FindManyAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var removedTriggers = removed.ToList();
        var addedTriggers = added.ToList();
        
        if (removedTriggers.Any())
        {
            var filter = new TriggerFilter {Ids = removedTriggers.Select(r => r.Id).ToList()};
            await DeleteManyAsync(filter, cancellationToken);
        }

        if(addedTriggers.Any())
            await _mongoStore.SaveManyAsync(addedTriggers, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    
    private static IMongoQueryable<StoredTrigger> Filter(IMongoQueryable<StoredTrigger> queryable, TriggerFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<StoredTrigger>)!;
}