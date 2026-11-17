using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
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
public class MemoryBookmarkQueueDeadLetterStore(MemoryStore<BookmarkQueueDeadLetterItem> store, ITenantAccessor? tenantAccessor) : IBookmarkQueueDeadLetterStore
{
    /// <summary>
    /// Creates a store for a single tenant.
    /// </summary>
    public MemoryBookmarkQueueDeadLetterStore(MemoryStore<BookmarkQueueDeadLetterItem> store) : this(store, null)
    {
    }

    /// <inheritdoc />
    public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var owned = PrepareWrite(record);
            var existing = FindOriginal(owned);
            if (existing != null && existing.Id != record.Id)
                throw new InvalidOperationException($"A bookmark queue dead-letter item for original queue item '{record.OriginalQueueItemId}' already exists.");

            store.Save(owned, x => x.Id);
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
        lock (store.Sync)
        {
            var owned = PrepareWrite(record);
            var existing = FindOriginal(owned);
            if (existing != null)
                return Task.FromResult(Clone(existing));

            store.Add(owned, x => x.Id);
            return Task.FromResult(Clone(owned));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<BookmarkQueueDeadLetterItem>> AddOrGetExistingManyAsync(IEnumerable<BookmarkQueueDeadLetterItem> records, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var results = new List<BookmarkQueueDeadLetterItem>();

            foreach (var record in records.Select(PrepareWrite).ToList())
            {
                var existing = FindOriginal(record);
                if (existing != null)
                {
                    results.Add(Clone(existing));
                    continue;
                }

                store.Add(record, x => x.Id);
                results.Add(Clone(record));
            }

            return Task.FromResult<IReadOnlyCollection<BookmarkQueueDeadLetterItem>>(results);
        }
    }

    /// <inheritdoc />
    public Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var entity = MemoryRuntimeRecordOwnership.Owned(store.Queryable, tenantAccessor).FirstOrDefault(x => x.Id == id);
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
        lock (store.Sync)
        {
            entity = store.Query(query => Filter(query, filter)).Select(Clone).FirstOrDefault();
        }

        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        Page<BookmarkQueueDeadLetterItem> entities;
        lock (store.Sync)
        {
            var page = store.Query(query => MemoryRuntimeRecordOwnership.Visible(query, tenantAccessor).OrderBy(orderBy)).Paginate(pageArgs);
            entities = page with { Items = page.Items.Select(Clone).ToList() };
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        Page<BookmarkQueueDeadLetterItem> entities;
        lock (store.Sync)
        {
            var page = store.Query(query => Filter(query, filter).OrderBy(orderBy)).Paginate(pageArgs);
            entities = page with { Items = page.Items.Select(Clone).ToList() };
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync<TOrderBy>(BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        IEnumerable<BookmarkQueueDeadLetterItem> entities;
        lock (store.Sync)
        {
            entities = store.Query(query => Filter(query, filter).OrderBy(orderBy).Paginate(pageArgs)).Select(Clone).ToList();
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        IEnumerable<BookmarkQueueDeadLetterItem> entities;
        lock (store.Sync)
        {
            entities = store.Query(query => Filter(query, filter)).Select(Clone).ToList();
        }

        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var ids = store.Query(query => filter.Apply(MemoryRuntimeRecordOwnership.Owned(query, tenantAccessor))).Select(x => x.Id).ToList();
            return Task.FromResult(store.DeleteMany(ids));
        }
    }

    private IQueryable<BookmarkQueueDeadLetterItem> Filter(IQueryable<BookmarkQueueDeadLetterItem> query, BookmarkQueueDeadLetterFilter filter) =>
        filter.Apply(MemoryRuntimeRecordOwnership.Visible(query, tenantAccessor, filter.TenantAgnostic));

    private BookmarkQueueDeadLetterItem PrepareWrite(BookmarkQueueDeadLetterItem record)
    {
        var copy = Clone(record);
        copy.TenantId = MemoryRuntimeRecordOwnership.GetWriteTenant(record, tenantAccessor);
        MemoryRuntimeRecordOwnership.DemandOwner(store.FindById(record.Id), tenantAccessor);
        return copy;
    }

    private BookmarkQueueDeadLetterItem? FindOriginal(BookmarkQueueDeadLetterItem record) =>
        MemoryRuntimeRecordOwnership.Owned(store.Queryable, tenantAccessor)
            .FirstOrDefault(existing => existing.OriginalQueueItemId == record.OriginalQueueItemId);

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
            UnreadableOptions = item.UnreadableOptions,
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
