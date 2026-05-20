using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Options;
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
        lock (_lock)
        {
            var existing = store.Find(x => x.OriginalQueueItemId == record.OriginalQueueItemId && x.Id != record.Id);
            if (existing != null)
                throw new InvalidOperationException($"A bookmark queue dead-letter item for original queue item '{record.OriginalQueueItemId}' already exists.");

            store.Save(Clone(record), x => x.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        await AddOrGetExistingAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BookmarkQueueDeadLetterItem> AddOrGetExistingAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var existing = store.Find(x => x.OriginalQueueItemId == record.OriginalQueueItemId);
            if (existing != null)
                return Task.FromResult(Clone(existing));

            store.Add(Clone(record), x => x.Id);
            return Task.FromResult(Clone(record));
        }
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
            return Task.FromResult<BookmarkQueueDeadLetterItem?>(Clone(entity));
        }
    }

    /// <inheritdoc />
    public Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        BookmarkQueueDeadLetterItem? entity;
        lock (_lock)
        {
            entity = store.Query(query => Filter(query, filter)).Select(Clone).FirstOrDefault();
        }

        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        Page<BookmarkQueueDeadLetterItem> entities;
        lock (_lock)
        {
            var page = store.Query(query => query.OrderBy(orderBy)).Paginate(pageArgs);
            entities = page with { Items = page.Items.Select(Clone).ToList() };
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        Page<BookmarkQueueDeadLetterItem> entities;
        lock (_lock)
        {
            var page = store.Query(query => Filter(query, filter).OrderBy(orderBy)).Paginate(pageArgs);
            entities = page with { Items = page.Items.Select(Clone).ToList() };
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        IEnumerable<BookmarkQueueDeadLetterItem> entities;
        lock (_lock)
        {
            entities = store.Query(query => Filter(query, filter)).Select(Clone).ToList();
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var ids = store.Query(query => Filter(query, filter)).Select(x => x.Id).ToList();
            return Task.FromResult(store.DeleteMany(ids));
        }
    }

    private static IQueryable<BookmarkQueueDeadLetterItem> Filter(IQueryable<BookmarkQueueDeadLetterItem> query, BookmarkQueueDeadLetterFilter filter) => filter.Apply(query);

    private static BookmarkQueueDeadLetterItem Clone(BookmarkQueueDeadLetterItem item)
    {
        return new()
        {
            Id = item.Id,
            TenantId = item.TenantId,
            OriginalQueueItemId = item.OriginalQueueItemId,
            WorkflowInstanceId = item.WorkflowInstanceId,
            CorrelationId = item.CorrelationId,
            BookmarkId = item.BookmarkId,
            StimulusHash = item.StimulusHash,
            ActivityInstanceId = item.ActivityInstanceId,
            ActivityTypeName = item.ActivityTypeName,
            Options = Clone(item.Options),
            OriginalCreatedAt = item.OriginalCreatedAt,
            DeadLetteredAt = item.DeadLetteredAt,
            Reason = item.Reason,
            DeliveryAttempts = item.DeliveryAttempts,
            LastAttemptedAt = item.LastAttemptedAt,
            LastErrorType = item.LastErrorType,
            LastErrorMessage = item.LastErrorMessage,
            CanReplay = item.CanReplay,
            ReplayedAt = item.ReplayedAt,
            ReplayedQueueItemId = item.ReplayedQueueItemId
        };
    }

    private static ResumeBookmarkOptions? Clone(ResumeBookmarkOptions? options)
    {
        if (options == null)
            return null;

        return new()
        {
            Input = options.Input == null ? null : new Dictionary<string, object>(options.Input),
            Properties = options.Properties == null ? null : new Dictionary<string, object>(options.Properties)
        };
    }
}
