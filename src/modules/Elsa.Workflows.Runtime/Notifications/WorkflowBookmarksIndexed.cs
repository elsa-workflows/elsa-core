using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Models.Notifications;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowBookmarksIndexed(IndexedWorkflowBookmarks IndexedWorkflowBookmarks) : INotification;