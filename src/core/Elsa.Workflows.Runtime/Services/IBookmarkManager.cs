using Elsa.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides methods to add and remove bookmarks.
/// </summary>
public interface IBookmarkManager
{
    Task DeleteBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default);
    Task SaveBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default);
}