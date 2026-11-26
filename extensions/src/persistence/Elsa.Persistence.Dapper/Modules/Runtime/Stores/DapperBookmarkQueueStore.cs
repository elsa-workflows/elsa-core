using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper-based <see cref="IBookmarkQueueStore"/> implementation.
/// </summary>
[UsedImplicitly]
internal class DapperBookmarkQueueStore(Store<BookmarkQueueItemRecord> store, IPayloadSerializer payloadSerializer) : IBookmarkQueueStore
{
    /// <inheritdoc />
    public async Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.SaveAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.AddAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), filter.TenantAgnostic, cancellationToken);
        return record != null ? Map(record) : null;
    }

    public async Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), filter.TenantAgnostic, cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BookmarkQueueItem>> FindManyAsync<TOrderBy>(BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var records = await store.FindPageItemsAsync(q => ApplyFilter(q, filter), pageArgs, GetOrderFields(orderBy), filter.TenantAgnostic, cancellationToken);
        return Map(records).ToList();
    }

    public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(null, pageArgs, GetOrderFields(orderBy), cancellationToken);
        return Map(records);
    }

    public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, GetOrderFields(orderBy), filter.TenantAgnostic, cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>?> ListTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        var tenantIds = await store.ListTenantIdsAsync(cancellationToken);
        return tenantIds.Select(x => x.NormalizeTenantId()).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private static OrderField[] GetOrderFields<TOrderBy>(BookmarkQueueItemOrder<TOrderBy> order)
    {
        var primary = new OrderField(order.KeySelector.GetPropertyName(), order.Direction);
        return primary.Field == nameof(BookmarkQueueItemRecord.Id)
            ? [primary]
            : [primary, new(nameof(BookmarkQueueItemRecord.Id), OrderDirection.Ascending)];
    }

    private void ApplyFilter(ParameterizedQuery query, BookmarkQueueFilter filter)
    {
        query
            .Is(nameof(BookmarkQueueItemRecord.Id), filter.Id)
            .In(nameof(BookmarkQueueItemRecord.Id), filter.Ids)
            .Is(nameof(BookmarkQueueItemRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .Is(nameof(BookmarkQueueItemRecord.BookmarkId), filter.BookmarkId)
            .Is(nameof(BookmarkQueueItemRecord.StimulusHash), filter.BookmarkHash)
            .Is(nameof(BookmarkQueueItemRecord.ActivityInstanceId), filter.ActivityInstanceId)
            .Is(nameof(BookmarkQueueItemRecord.ActivityTypeName), filter.ActivityTypeName)
            .LessThan(nameof(BookmarkQueueItemRecord.CreatedAt), filter.CreatedAtLessThan)
            ;
    }

    private Page<BookmarkQueueItem> Map(Page<BookmarkQueueItemRecord> records) => new(Map(records.Items).ToList(), records.TotalCount);
    private IEnumerable<BookmarkQueueItem> Map(IEnumerable<BookmarkQueueItemRecord> source) => source.Select(Map);
    private IEnumerable<BookmarkQueueItemRecord> Map(IEnumerable<BookmarkQueueItem> source) => source.Select(Map);

    private BookmarkQueueItemRecord Map(BookmarkQueueItem source)
    {
        return new BookmarkQueueItemRecord
        {
            Id = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            BookmarkId = source.BookmarkId,
            StimulusHash = source.StimulusHash,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            SerializedOptions = BookmarkQueueOptionsSerialization.Write(payloadSerializer, source.Options, source.UnreadableOptions),
            CreatedAt = source.CreatedAt,
            DeliveryAttempts = source.DeliveryAttempts,
            LastAttemptedAt = source.LastAttemptedAt,
            LastErrorType = source.LastErrorType,
            LastErrorMessage = source.LastErrorMessage,
            TenantId = source.TenantId
        };
    }

    private BookmarkQueueItem Map(BookmarkQueueItemRecord source)
    {
        var (options, unreadable) = BookmarkQueueOptionsSerialization.Read(payloadSerializer, source.SerializedOptions);
        return new BookmarkQueueItem
        {
            Id = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            BookmarkId = source.BookmarkId,
            StimulusHash = source.StimulusHash,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            Options = options,
            UnreadableOptions = unreadable,
            CreatedAt = source.CreatedAt,
            DeliveryAttempts = source.DeliveryAttempts,
            LastAttemptedAt = source.LastAttemptedAt,
            LastErrorType = source.LastErrorType,
            LastErrorMessage = source.LastErrorMessage,
            TenantId = source.TenantId
        };
    }
}
