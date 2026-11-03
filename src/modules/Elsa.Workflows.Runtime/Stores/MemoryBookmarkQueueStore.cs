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
public class MemoryBookmarkQueueStore(MemoryStore<BookmarkQueueItem> store) : IBookmarkQueueStore
{
    /// <inheritdoc />
    public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        store.Add(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => ApplyOrder(query, orderBy)).Paginate(pageArgs);
        return Task.FromResult(entities);
    }

    public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => ApplyOrder(Filter(query, filter), orderBy)).Paginate(pageArgs);
        return Task.FromResult(entities);
    }

    public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => Filter(query, filter)).AsEnumerable();
        return Task.FromResult(entities);
    }
    
    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.Id);
        return store.DeleteMany(ids);
    }
    
    /// <summary>
    /// Uses the unique ID to keep page boundaries stable when primary sort values are equal.
    /// </summary>
    private static IOrderedQueryable<BookmarkQueueItem> ApplyOrder<TOrderBy>(IQueryable<BookmarkQueueItem> query, BookmarkQueueItemOrder<TOrderBy> orderBy)
    {
        var ordered = orderBy.Direction == OrderDirection.Ascending
            ? query.OrderBy(orderBy.KeySelector)
            : query.OrderByDescending(orderBy.KeySelector);
        return ordered.ThenBy(x => x.Id);
    }

    private static IQueryable<BookmarkQueueItem> Filter(IQueryable<BookmarkQueueItem> query, BookmarkQueueFilter filter) => filter.Apply(query);
}