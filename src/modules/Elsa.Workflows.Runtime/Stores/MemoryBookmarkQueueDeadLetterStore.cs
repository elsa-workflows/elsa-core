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
public class MemoryBookmarkQueueDeadLetterStore(MemoryStore<BookmarkQueueDeadLetterItem> store) : IBookmarkQueueDeadLetterStore
{
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        store.Add(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var entity = store.Find(x => x.Id == id);
            if (entity == null || !entity.CanReplay || entity.ReplayedAt != null)
                return Task.FromResult<BookmarkQueueDeadLetterItem?>(null);

            entity.ReplayedAt = replayedAt;
            entity.ReplayedQueueItemId = queueItemId;
            entity.CanReplay = false;
            store.Save(entity, x => x.Id);
            return Task.FromResult<BookmarkQueueDeadLetterItem?>(entity);
        }
    }

    /// <inheritdoc />
    public Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => query.OrderBy(orderBy)).Paginate(pageArgs);
        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => Filter(query, filter).OrderBy(orderBy)).Paginate(pageArgs);
        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => Filter(query, filter)).AsEnumerable();
        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.Id);
        return store.DeleteMany(ids);
    }

    private static IQueryable<BookmarkQueueDeadLetterItem> Filter(IQueryable<BookmarkQueueDeadLetterItem> query, BookmarkQueueDeadLetterFilter filter) => filter.Apply(query);
}
