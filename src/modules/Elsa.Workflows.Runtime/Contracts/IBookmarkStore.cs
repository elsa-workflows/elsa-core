using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides access to stored bookmarks.
/// </summary>
public interface IBookmarkStore
{
    /// <summary>
    /// Adds or updates the specified <see cref="StoredBookmark"/> in the persistence store.
    /// </summary>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds or updates the specified set of <see cref="StoredBookmark"/> objects in the persistence store.
    /// </summary>
    /// <remarks>
    /// If a record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the first bookmark matching the specified filter.
    /// </summary>
    ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of bookmarks matching the specified filter.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of bookmarks matching the specified filter.
    /// </summary>
    /// <remarks>
    /// The default implementation materializes all matching bookmarks and pages them in memory. Stores backed by external persistence should override this method.
    /// </remarks>
    async ValueTask<Page<StoredBookmark>> FindManyAsync(BookmarkFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var records = (await FindManyAsync(filter, cancellationToken)).OrderBy(x => x.Id).ToList();
        IEnumerable<StoredBookmark> page = records;

        if (pageArgs.Offset.HasValue)
            page = page.Skip(pageArgs.Offset.Value);

        if (pageArgs.Limit.HasValue)
            page = page.Take(pageArgs.Limit.Value);

        return Page.Of(page.ToList(), records.Count);
    }

    /// <summary>
    /// Deletes a set of bookmarks matching the specified filter.
    /// </summary>
    ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);
}