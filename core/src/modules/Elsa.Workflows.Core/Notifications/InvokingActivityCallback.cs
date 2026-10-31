using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

public record InvokingActivityCallback(ActivityExecutionContext Parent, ActivityExecutionContext Child) : INotification;