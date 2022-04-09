using Elsa.Mediator.Contracts;
using Elsa.Runtime.Models;

namespace Elsa.Runtime.Notifications;

public record WorkflowBookmarksIndexed(IndexedWorkflowBookmarks IndexedWorkflowBookmarks) : INotification;