using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A notification that is sent when an activity is cancelled.
/// </summary>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
public record ActivityCancelled(ActivityExecutionContext ActivityExecutionContext) : INotification;