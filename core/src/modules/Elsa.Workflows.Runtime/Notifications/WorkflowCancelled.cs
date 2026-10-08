using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowCancelled(string WorkflowInstanceId) : INotification;