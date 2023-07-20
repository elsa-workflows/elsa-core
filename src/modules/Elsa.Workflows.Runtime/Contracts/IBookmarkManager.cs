using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Manages bookmarks.
/// </summary>
public interface IBookmarkManager
{
    /// <summary>
    /// Deletes all bookmarks matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of deleted bookmarks.</returns>
    Task<long> DeleteManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default);
}