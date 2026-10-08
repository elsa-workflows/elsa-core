using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A notification that is sent when the bookmarks of a workflow instance have been indexed.
/// </summary>
/// <param name="IndexedWorkflowBookmarks">The bookmarks that were added, removed, or unchanged.</param>
public record WorkflowBookmarksIndexed(IndexedWorkflowBookmarks IndexedWorkflowBookmarks) : INotification;

