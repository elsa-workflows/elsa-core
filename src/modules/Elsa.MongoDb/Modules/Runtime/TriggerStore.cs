using Elsa.Common.Models;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// <inheritdoc />
[UsedImplicitly]
public class MongoTriggerStore(MongoDbStore<StoredTrigger> mongoDbStore) : ITriggerStore
{
    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        await mongoDbStore.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        await mongoDbStore.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await mongoDbStore.FindAsync(query => Filter(query, filter), filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await mongoDbStore.FindManyAsync(query => Filter(query, filter), filter.TenantAgnostic, cancellationToken);
    }

    public ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

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
            await mongoDbStore.SaveManyAsync(addedTriggers, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await mongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, filter.TenantAgnostic, cancellationToken);
    }

    private static IQueryable<StoredTrigger> Filter(IQueryable<StoredTrigger> queryable, TriggerFilter filter)
    {
        return filter.Apply(queryable);
    }
}