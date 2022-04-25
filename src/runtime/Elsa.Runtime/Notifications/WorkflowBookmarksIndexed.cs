using Elsa.Mediator.Services;
using Elsa.Runtime.Models;

namespace Elsa.Runtime.Notifications;

public record WorkflowBookmarksIndexed(IndexedWorkflowBookmarks IndexedWorkflowBookmarks) : INotification;