using Elsa.Common.Models;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// A Dapper-based <see cref="IBookmarkQueueStore"/> implementation.
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
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record != null ? Map(record) : default;
    }

    public async Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records);
    }

    public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var records = await store.ListAsync(pageArgs, orderBy.KeySelector.GetPropertyName(), orderBy.Direction, cancellationToken);
        return Map(records);
    }

    public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, orderBy.KeySelector.GetPropertyName(), orderBy.Direction, cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, BookmarkQueueFilter filter)
    {
        query
            .Is(nameof(BookmarkQueueItemRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .Is(nameof(BookmarkQueueItemRecord.BookmarkId), filter.BookmarkId)
            .Is(nameof(BookmarkQueueItemRecord.StimulusHash), filter.BookmarkHash)
            .Is(nameof(BookmarkQueueItemRecord.ActivityInstanceId), filter.ActivityInstanceId)
            .Is(nameof(BookmarkQueueItemRecord.ActivityTypeName), filter.ActivityTypeName)
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
            BookmarkId = source.BookmarkId,
            StimulusHash = source.StimulusHash,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            SerializedOptions = source.Options != null ? payloadSerializer.Serialize(source.Options) : default,
            CreatedAt = source.CreatedAt,
            TenantId = source.TenantId
        };
    }

    private BookmarkQueueItem Map(BookmarkQueueItemRecord source)
    {
        return new BookmarkQueueItem
        {
            Id = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            BookmarkId = source.BookmarkId,
            StimulusHash = source.StimulusHash,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            Options = source.SerializedOptions != null ? payloadSerializer.Deserialize<ResumeBookmarkOptions>(source.SerializedOptions) : default,
            CreatedAt = source.CreatedAt,
            TenantId = source.TenantId
        };
    }
}