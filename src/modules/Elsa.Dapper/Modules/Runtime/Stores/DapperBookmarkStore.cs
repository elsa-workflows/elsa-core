using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

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
        return record != null ? Map(record) : default;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), filter.TenantAgnostic, cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, BookmarkFilter filter)
    {
        query
            .Is(nameof(StoredBookmarkRecord.Hash), filter.Hash)
            .In(nameof(StoredBookmarkRecord.Hash), filter.Hashes)
            .Is(nameof(StoredBookmarkRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .In(nameof(StoredBookmarkRecord.WorkflowInstanceId), filter.WorkflowInstanceIds)
            .Is(nameof(StoredBookmarkRecord.CorrelationId), filter.CorrelationId)
            .Is(nameof(StoredBookmarkRecord.ActivityTypeName), filter.ActivityTypeName)
            .In(nameof(StoredBookmarkRecord.ActivityTypeName), filter.ActivityTypeNames)
            .Is(nameof(StoredBookmarkRecord.ActivityInstanceId), filter.ActivityInstanceId)
            ;
    }

    private IEnumerable<StoredBookmark> Map(IEnumerable<StoredBookmarkRecord> source) => source.Select(Map);
    private IEnumerable<StoredBookmarkRecord> Map(IEnumerable<StoredBookmark> source) => source.Select(Map);

    private StoredBookmarkRecord Map(StoredBookmark source)
    {
        return new StoredBookmarkRecord
        {
            Id = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            Hash = source.Hash,
            SerializedPayload = source.Payload != null ? payloadSerializer.Serialize(source.Payload) : default,
            SerializedMetadata = source.Metadata != null ? payloadSerializer.Serialize(source.Metadata) : default,
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
            ActivityTypeName = source.ActivityTypeName,
            Hash = source.Hash,
            Payload = source.SerializedPayload != null ? payloadSerializer.Deserialize(source.SerializedPayload) : default,
            Metadata = source.SerializedMetadata != null ? payloadSerializer.Deserialize<Dictionary<string, string>>(source.SerializedMetadata) : default,
            CreatedAt = source.CreatedAt,
            TenantId = source.TenantId
        };
    }
}