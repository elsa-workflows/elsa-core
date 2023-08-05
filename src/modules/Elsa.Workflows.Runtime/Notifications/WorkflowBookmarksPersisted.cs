using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published when the bookmarks of a workflow instance have been persisted.
/// </summary>
/// <param name="Context">The workflow execution context.</param>
/// <param name="Diff">The bookmarks that were added, removed, or unchanged.</param>
public record WorkflowBookmarksPersisted(WorkflowExecutionContext Context, Diff<Bookmark> Diff) : INotification;