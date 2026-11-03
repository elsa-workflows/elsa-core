using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published after bookmark changes and the owning workflow instance state have been saved,
/// before deferred tasks execute.
/// </summary>
/// <param name="Diff">The bookmark changes persisted by this commit.</param>
public record WorkflowBookmarksCommitted(Diff<Bookmark> Diff) : INotification;
