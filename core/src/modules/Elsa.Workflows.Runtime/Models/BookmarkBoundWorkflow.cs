using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a workflow bound to one or more bookmarks.
/// </summary>
public record BookmarkBoundWorkflow(string WorkflowInstanceId, ICollection<StoredBookmark> Bookmarks);