using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides methods to add and remove bookmarks.
/// </summary>
public interface IBookmarkManager
{
    Task DeleteBookmarksAsync(IEnumerable<WorkflowBookmark> workflowBookmarks, CancellationToken cancellationToken = default);
    Task SaveBookmarksAsync(IEnumerable<WorkflowBookmark> workflowBookmarks, CancellationToken cancellationToken = default);
}