using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper-based <see cref="IBookmarkStore"/> implementation.
/// </summary>
[UsedImplicitly]
internal class DapperBookmarkStore(Store<StoredBookmarkRecord> store, IPayloadSerializer payloadSerializer) : IBookmarkStore
{
    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.SaveAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        var mappedRecords = Map(records);
        await store.SaveManyAsync(mappedRecords, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record != null ? Map(record) : null;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), filter.TenantAgnostic, cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<Page<StoredBookmark>> FindManyAsync(BookmarkFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var page = await store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, nameof(StoredBookmarkRecord.Id), OrderDirection.Ascending, filter.TenantAgnostic, cancellationToken);
        return Map(page);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, BookmarkFilter filter)
    {
        query
            .Is(nameof(StoredBookmarkRecord.Id), filter.BookmarkId)
            .In(nameof(StoredBookmarkRecord.Id), filter.BookmarkIds)
            .Is(nameof(StoredBookmarkRecord.Hash), filter.Hash)
            .In(nameof(StoredBookmarkRecord.Hash), filter.Hashes)
            .Is(nameof(StoredBookmarkRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .In(nameof(StoredBookmarkRecord.WorkflowInstanceId), filter.WorkflowInstanceIds)
            .Is(nameof(StoredBookmarkRecord.CorrelationId), filter.CorrelationId)
            .Is(nameof(StoredBookmarkRecord.ActivityTypeName), filter.Name)
            .In(nameof(StoredBookmarkRecord.ActivityTypeName), filter.Names)
            .Is(nameof(StoredBookmarkRecord.ActivityInstanceId), filter.ActivityInstanceId)
            ;
    }

    private IEnumerable<StoredBookmark> Map(IEnumerable<StoredBookmarkRecord> source) => source.Select(Map);
    private Page<StoredBookmark> Map(Page<StoredBookmarkRecord> source) => new(source.Items.Select(Map).ToList(), source.TotalCount);
    private IEnumerable<StoredBookmarkRecord> Map(IEnumerable<StoredBookmark> source) => source.Select(Map);

    private StoredBookmarkRecord Map(StoredBookmark source)
    {
        return new StoredBookmarkRecord
        {
            Id = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.Name,
            Hash = source.Hash,
            SerializedPayload = source.Payload != null ? payloadSerializer.Serialize(source.Payload) : null,
            SerializedMetadata = source.Metadata != null ? payloadSerializer.Serialize(source.Metadata) : null,
            CreatedAt = source.CreatedAt,
            TenantId = source.TenantId
        };
    }

    private StoredBookmark Map(StoredBookmarkRecord source)
    {
        return new StoredBookmark
        {
            Id = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            Name = source.ActivityTypeName,
            Hash = source.Hash,
            Payload = source.SerializedPayload != null ? payloadSerializer.Deserialize(source.SerializedPayload) : null,
            Metadata = source.SerializedMetadata != null ? payloadSerializer.Deserialize<Dictionary<string, string>>(source.SerializedMetadata) : null,
            CreatedAt = source.CreatedAt,
            TenantId = source.TenantId
        };
    }
}