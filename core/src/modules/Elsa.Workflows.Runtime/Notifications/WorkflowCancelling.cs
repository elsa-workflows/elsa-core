using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowCancelling(string WorkflowInstanceId) : INotification;