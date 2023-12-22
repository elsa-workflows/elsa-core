using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Provides access to stored bookmarks.
/// </summary>
public interface IBookmarkStore
{
    /// <summary>
    /// Adds or updates the specified bookmark. 
    /// </summary>
    ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds or updates the specified bookmarks.
    /// </summary>
    ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a set of bookmarks matching the specified filter.
    /// </summary>
    ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a set of bookmarks matching the specified filter.
    /// </summary>
    ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);
}