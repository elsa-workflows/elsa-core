using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class EFCoreBookmarkStore : IBookmarkStore
{
    private readonly Store<RuntimeElsaDbContext, StoredBookmark> _store;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreBookmarkStore(Store<RuntimeElsaDbContext, StoredBookmark> store, IPayloadSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, s => s.BookmarkId, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, OnLoadAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(filter.Apply, cancellationToken);

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, StoredBookmark entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload != null ? _serializer.Serialize(entity.Payload) : default;
        dbContext.Entry(entity).Property("SerializedMetadata").CurrentValue = entity.Metadata != null ? _serializer.Serialize(entity.Metadata) : default;
        return default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, StoredBookmark? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return default;

        var payloadJson = dbContext.Entry(entity).Property<string>("SerializedPayload").CurrentValue;
        var metadataJson = dbContext.Entry(entity).Property<string>("SerializedMetadata").CurrentValue;
        entity.Payload = !string.IsNullOrEmpty(payloadJson) ? _serializer.Deserialize(payloadJson) : null;
        entity.Metadata = !string.IsNullOrEmpty(metadataJson) ? _serializer.Deserialize<Dictionary<string, string>>(metadataJson) : null;

        return default;
    }
}