using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// A MongoDb implementation of <see cref="IBookmarkQueueStore"/>.
[UsedImplicitly]
public class MongoBookmarkQueueStore(MongoDbStore<BookmarkQueueItem> mongoDbStore) : IBookmarkQueueStore
{
    /// <inheritdoc />
    public async Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        await mongoDbStore.SaveAsync(record, s => s.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken)
    {
        await mongoDbStore.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        return await mongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var results = await mongoDbStore.FindManyAsync(query => Paginate(Order(query, orderBy), pageArgs), cancellationToken);
        var count = await mongoDbStore.CountAsync(queryable => Order(queryable, orderBy), cancellationToken);
        return Page.Of(results.ToList(), count);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        return await mongoDbStore.DeleteWhereAsync<string>(query => Filter(query, filter), x => x.Id, cancellationToken);
    }

    private IQueryable<BookmarkQueueItem> Filter(IQueryable<BookmarkQueueItem> queryable, BookmarkQueueFilter filter)
    {
        return filter.Apply(queryable);
    }
    
    private IQueryable<BookmarkQueueItem> Order<TOrderBy>(IQueryable<BookmarkQueueItem> queryable, BookmarkQueueItemOrder<TOrderBy> order)
    {
        return queryable.OrderBy(order);
    }

    private IQueryable<BookmarkQueueItem> Paginate(IQueryable<BookmarkQueueItem> queryable, PageArgs pageArgs)
    {
        return queryable.Paginate(pageArgs);
    }
}