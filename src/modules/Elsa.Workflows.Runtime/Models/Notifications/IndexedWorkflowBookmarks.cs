using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Models.Notifications;

/// <summary>
/// Contains the bookmarks that were added, removed, or unchanged. 
/// </summary>
/// <param name="InstanceId">The workflow instance ID.</param>
/// <param name="AddedBookmarks">The bookmarks that were added.</param>
/// <param name="RemovedBookmarks">The bookmarks that were removed.</param>
/// <param name="UnchangedBookmarks">The bookmarks that were unchanged.</param>
public record IndexedWorkflowBookmarks(
    string InstanceId, 
    ICollection<Bookmark> AddedBookmarks, 
    ICollection<Bookmark> RemovedBookmarks, 
    ICollection<Bookmark> UnchangedBookmarks);