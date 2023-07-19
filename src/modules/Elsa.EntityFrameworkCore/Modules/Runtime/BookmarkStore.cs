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
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, s => s.BookmarkId, SaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, LoadAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
    
    private ValueTask<StoredBookmark> SaveAsync(RuntimeElsaDbContext dbContext, StoredBookmark entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("Data").CurrentValue = entity.Payload != null ? _serializer.Serialize(entity.Payload) : default;
        return new(entity);
    }

    private ValueTask<StoredBookmark?> LoadAsync(RuntimeElsaDbContext dbContext, StoredBookmark? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return ValueTask.FromResult(entity);

        var json = dbContext.Entry(entity).Property<string>("Data").CurrentValue;
        entity.Payload = !string.IsNullOrEmpty(json) ? _serializer.Deserialize(json) : null;

        return new(entity);
    }
}