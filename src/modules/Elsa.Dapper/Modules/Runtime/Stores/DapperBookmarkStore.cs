using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper-based <see cref="IBookmarkStore"/> implementation.
/// </summary>
public class DapperBookmarkStore : IBookmarkStore
{
    private readonly IPayloadSerializer _payloadSerializer;
    private const string TableName = "Bookmarks";
    private const string PrimaryKeyName = "Id";
    private readonly Store<StoredBookmarkRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperBookmarkStore"/> class.
    /// </summary>
    public DapperBookmarkStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _store = new Store<StoredBookmarkRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await _store.SaveAsync(mappedRecord, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
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

    private StoredBookmarkRecord Map(StoredBookmark source)
    {
        return new StoredBookmarkRecord
        {
            Id = source.BookmarkId,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            Hash = source.Hash,
            SerializedPayload = source.Payload != null ? _payloadSerializer.Serialize(source.Payload) : default,
            SerializedMetadata = source.Metadata != null ? _payloadSerializer.Serialize(source.Metadata) : default,
            CreatedAt = source.CreatedAt
        };
    }

    private StoredBookmark Map(StoredBookmarkRecord source)
    {
        return new StoredBookmark
        {
            BookmarkId = source.Id,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityTypeName = source.ActivityTypeName,
            Hash = source.Hash,
            Payload = source.SerializedPayload != null ? _payloadSerializer.Deserialize(source.SerializedPayload) : default,
            Metadata = source.SerializedMetadata != null ? _payloadSerializer.Deserialize<Dictionary<string, string>>(source.SerializedMetadata) : default,
            CreatedAt = source.CreatedAt
        };
    }
}