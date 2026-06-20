using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Stores;

/// <inheritdoc />
[UsedImplicitly]
public class MemoryBookmarkStore(MemoryStore<StoredBookmark> store) : IBookmarkStore
{
    /// <inheritdoc />
    public ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        store.Save(record, x => x.Id);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        store.SaveMany(records, x => x.Id);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return new(entity);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = store.Query(query => Filter(query, filter)).AsEnumerable();
        return new(entities);
    }

    /// <inheritdoc />
    public ValueTask<Page<StoredBookmark>> FindManyAsync(BookmarkFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, filter)).LongCount();
        var result = store.Query(query => Filter(query, filter).OrderBy(x => x.Id).Paginate(pageArgs)).ToList();
        return new(Page.Of(result, count));
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.Id);
        return store.DeleteMany(ids);
    }
    
    private static IQueryable<StoredBookmark> Filter(IQueryable<StoredBookmark> query, BookmarkFilter filter) => filter.Apply(query);
}