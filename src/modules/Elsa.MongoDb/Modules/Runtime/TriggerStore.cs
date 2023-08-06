using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// <inheritdoc />
public class MongoTriggerStore : ITriggerStore
{
    private readonly MongoDbStore<StoredTrigger> _mongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoTriggerStore"/> class.
    /// </summary>
    public MongoTriggerStore(MongoDbStore<StoredTrigger> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken);

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
            await _mongoDbStore.SaveManyAsync(addedTriggers, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default) =>
        await _mongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);
    
    private static IMongoQueryable<StoredTrigger> Filter(IMongoQueryable<StoredTrigger> queryable, TriggerFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<StoredTrigger>)!;
}