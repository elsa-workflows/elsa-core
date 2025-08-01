using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

public record ActivityCompleted(ActivityExecutionContext ActivityExecutionContext) : INotification;