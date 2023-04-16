using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

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
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, s => s.BookmarkId, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, LoadAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
    
    private async ValueTask<StoredBookmark> SaveAsync(RuntimeElsaDbContext dbContext, StoredBookmark entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("PayloadData").CurrentValue = entity.Payload != null ? await _serializer.SerializeAsync(entity.Payload, cancellationToken) : default;
        return entity;
    }

    private async ValueTask<StoredBookmark?> LoadAsync(RuntimeElsaDbContext dbContext, StoredBookmark? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return entity;

        var json = dbContext.Entry(entity).Property<string>("PayloadData").CurrentValue;
        entity.Payload = !string.IsNullOrEmpty(json) ? await _serializer.DeserializeAsync(json, cancellationToken) : null;

        return entity;
    }
}