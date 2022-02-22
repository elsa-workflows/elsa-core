using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Contracts;

/// <summary>
/// Provides methods to add and remove bookmarks.
/// </summary>
public interface IBookmarkManager
{
    Task DeleteBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default);
    Task SaveBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default);
}