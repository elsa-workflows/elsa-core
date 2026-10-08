using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Contains the bookmarks that were added, removed, or unchanged.
/// </summary>
/// <param name="WorkflowExecutionContext">The workflow execution context.</param>
/// <param name="AddedBookmarks">The bookmarks that were added.</param>
/// <param name="RemovedBookmarks">The bookmarks that were removed.</param>
/// <param name="UnchangedBookmarks">The bookmarks that were unchanged.</param>
public record IndexedWorkflowBookmarks(
    WorkflowExecutionContext WorkflowExecutionContext,
    ICollection<Bookmark> AddedBookmarks,
    ICollection<Bookmark> RemovedBookmarks,
    ICollection<Bookmark> UnchangedBookmarks);