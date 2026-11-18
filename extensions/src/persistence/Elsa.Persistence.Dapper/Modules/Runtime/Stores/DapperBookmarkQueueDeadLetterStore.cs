using System.Data.Common;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;

using Elsa.Persistence.Dapper.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Stores;

internal sealed class DapperBookmarkQueueDeadLetterStore(Store<BookmarkQueueDeadLetterRecord> store, IPayloadSerializer serializer) : IBookmarkQueueDeadLetterStore
{
    public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) =>
        store.SaveAsync(Map(record), cancellationToken);

    public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) =>
        store.AddAsync(Map(record), cancellationToken);

    public async Task<BookmarkQueueDeadLetterItem> AddOrGetExistingAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        var row = Map(record);
        try
        {
            await store.AddAsync(row, cancellationToken);
            record.TenantId = row.TenantId;
            return record;
        }
        catch (DbException exception) when (DapperExceptionClassifier.IsDuplicateKey(exception))
        {
            var existing = await FindAsync(new BookmarkQueueDeadLetterFilter { OriginalQueueItemId = row.OriginalQueueItemId }, cancellationToken);
            if (existing != null && existing.TenantId.NormalizeTenantId() == row.TenantId.NormalizeTenantId())
                return existing;

            throw;
        }
    }

    public async Task<IReadOnlyCollection<BookmarkQueueDeadLetterItem>> AddOrGetExistingManyAsync(IEnumerable<BookmarkQueueDeadLetterItem> records, CancellationToken cancellationToken = default)
    {
        var saved = new List<BookmarkQueueDeadLetterItem>();
        foreach (var record in records)
            saved.Add(await AddOrGetExistingAsync(record, cancellationToken));

        return saved;
    }

    public async Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default)
    {
        var found = await FindAsync(new BookmarkQueueDeadLetterFilter { Id = id }, cancellationToken);
        if (found is not { CanReplay: true, ReplayedAt: null })
            return null;

        var changes = new BookmarkQueueDeadLetterRecord { Id = id, ReplayedAt = replayedAt, ReplayedQueueItemId = queueItemId, CanReplay = false };
        var affected = await store.UpdateAsync(changes, [record => record.CanReplay, record => record.ReplayedAt!, record => record.ReplayedQueueItemId!],
            query => query.Is(nameof(BookmarkQueueDeadLetterRecord.Id), id).Is(nameof(BookmarkQueueDeadLetterRecord.CanReplay), true), cancellationToken);
        if (affected != 1)
            return null;

        found.CanReplay = false;
        found.ReplayedAt = replayedAt;
        found.ReplayedQueueItemId = queueItemId;
        return found;
    }

    public async Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        var row = await store.FindAsync(query => ApplyFilter(query, filter), filter.TenantAgnostic, cancellationToken);
        return row == null ? null : Map(row);
    }

    public async Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await store.FindManyAsync(query => ApplyFilter(query, filter), filter.TenantAgnostic, cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync<TOrderBy>(BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var rows = await store.FindPageItemsAsync(query => ApplyFilter(query, filter), pageArgs, OrderKey(order), order.Direction, filter.TenantAgnostic, cancellationToken);
        return rows.Select(Map).ToList();
    }

    public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        PageAsync(pageArgs, new BookmarkQueueDeadLetterFilter(), order, cancellationToken);

    public async Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await store.FindManyAsync(query => ApplyFilter(query, filter), pageArgs, OrderKey(order), order.Direction, filter.TenantAgnostic, cancellationToken);
        return new(page.Items.Select(Map).ToList(), page.TotalCount);
    }

    public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) =>
        store.DeleteAsync(query => ApplyFilter(query, filter), cancellationToken);

    private static string OrderKey<T>(BookmarkQueueDeadLetterItemOrder<T> order)
    {
        var key = order.KeySelector.GetPropertyName();
        return key == nameof(BookmarkQueueDeadLetterItem.OriginalCreatedAt) ? nameof(BookmarkQueueDeadLetterRecord.CreatedAt) : key;
    }

    private static void ApplyFilter(ParameterizedQuery query, BookmarkQueueDeadLetterFilter filter)
    {
        query.Is(nameof(BookmarkQueueDeadLetterRecord.Id), filter.Id)
            .In(nameof(BookmarkQueueDeadLetterRecord.Id), filter.Ids)
            .Is(nameof(BookmarkQueueDeadLetterRecord.OriginalQueueItemId), filter.OriginalQueueItemId)
            .In(nameof(BookmarkQueueDeadLetterRecord.OriginalQueueItemId), filter.OriginalQueueItemIds)
            .Is(nameof(BookmarkQueueDeadLetterRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .LessThan(nameof(BookmarkQueueDeadLetterRecord.DeadLetteredAt), filter.DeadLetteredAtLessThan);
    }

    private BookmarkQueueDeadLetterRecord Map(BookmarkQueueDeadLetterItem item) => new()
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
        SerializedOptions = BookmarkQueueOptionsSerialization.Write(serializer, item.Options, item.UnreadableOptions),
        CreatedAt = item.OriginalCreatedAt,
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

    private BookmarkQueueDeadLetterItem Map(BookmarkQueueDeadLetterRecord row)
    {
        var (options, unreadable) = BookmarkQueueOptionsSerialization.Read(serializer, row.SerializedOptions);
        return new()
        {
            Id = row.Id,
            TenantId = row.TenantId,
            OriginalQueueItemId = row.OriginalQueueItemId,
            WorkflowInstanceId = row.WorkflowInstanceId,
            CorrelationId = row.CorrelationId,
            BookmarkId = row.BookmarkId,
            StimulusHash = row.StimulusHash,
            ActivityInstanceId = row.ActivityInstanceId,
            ActivityTypeName = row.ActivityTypeName,
            Options = options,
            UnreadableOptions = unreadable,
            OriginalCreatedAt = row.CreatedAt,
            DeadLetteredAt = row.DeadLetteredAt,
            Reason = row.Reason,
            DeliveryAttempts = row.DeliveryAttempts,
            LastAttemptedAt = row.LastAttemptedAt,
            LastErrorType = row.LastErrorType,
            LastErrorMessage = row.LastErrorMessage,
            CanReplay = row.CanReplay && unreadable == null,
            ReplayedAt = row.ReplayedAt,
            ReplayedQueueItemId = row.ReplayedQueueItemId
        };
    }
}
