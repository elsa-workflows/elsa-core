using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Stores;

/// <inheritdoc />
[UsedImplicitly]
public class MemoryBookmarkStore : IBookmarkStore
{
    private readonly MemoryStore<StoredBookmark> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryBookmarkStore(MemoryStore<StoredBookmark> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.BookmarkId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        _store.SaveMany(records, x => x.BookmarkId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return new(entity);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter)).AsEnumerable();
        return new(entities);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.BookmarkId);
        return _store.DeleteMany(ids);
    }
    
    private static IQueryable<StoredBookmark> Filter(IQueryable<StoredBookmark> query, BookmarkFilter filter) => filter.Apply(query);
}