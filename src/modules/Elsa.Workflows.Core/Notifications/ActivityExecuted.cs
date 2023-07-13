using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A notification that is sent when an activity has executed.
/// </summary>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
public record ActivityExecuted(ActivityExecutionContext ActivityExecutionContext) : INotification;