using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published when the bookmarks of a workflow instance have been persisted.
/// </summary>
/// <param name="Context">The workflow execution context.</param>
/// <param name="Diff">The bookmarks that were added, removed, or unchanged.</param>
public record WorkflowBookmarksPersisted(Diff<Bookmark> Diff) : INotification;