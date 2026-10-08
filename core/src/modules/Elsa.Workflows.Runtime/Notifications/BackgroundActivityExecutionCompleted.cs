using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

public record BackgroundActivityExecutionCompleted(ActivityExecutionContext ActivityExecutionContext) : INotification;