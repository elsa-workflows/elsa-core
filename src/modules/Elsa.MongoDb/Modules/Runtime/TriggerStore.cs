using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

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

    public async ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(filter, pageArgs, new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending), cancellationToken);
    }

    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        var count = await mongoDbStore.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
        var results = await mongoDbStore.FindManyAsync(queryable => Paginate(Order(Filter(queryable, filter), order), pageArgs), cancellationToken).ToList();
        return new(results, count);
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
    
    private IQueryable<StoredTrigger> Order<TOrderBy>(IQueryable<StoredTrigger> queryable, StoredTriggerOrder<TOrderBy> order)
    {
        return queryable.OrderBy(order);
    }
    
    private IQueryable<StoredTrigger> Paginate(IQueryable<StoredTrigger> queryable, PageArgs pageArgs)
    {
        return queryable.Paginate(pageArgs);
    }
}