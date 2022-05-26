using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowBookmarksIndexed(IndexedWorkflowBookmarks IndexedWorkflowBookmarks) : INotification;