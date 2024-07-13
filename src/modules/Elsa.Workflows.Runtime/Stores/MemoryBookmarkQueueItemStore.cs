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
public class MemoryBookmarkQueueItemStore(MemoryStore<BookmarkQueueItem> store) : IBookmarkQueueItemStore
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
    public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueItemFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => query.OrderBy(orderBy)).Paginate(pageArgs);
        return Task.FromResult(entities);
    }

    public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueItemFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => Filter(query, filter)).AsEnumerable();
        return Task.FromResult(entities);
    }
    
    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueItemFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.Id);
        return store.DeleteMany(ids);
    }
    
    private static IQueryable<BookmarkQueueItem> Filter(IQueryable<BookmarkQueueItem> query, BookmarkQueueItemFilter filter) => filter.Apply(query);
}