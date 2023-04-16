using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
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