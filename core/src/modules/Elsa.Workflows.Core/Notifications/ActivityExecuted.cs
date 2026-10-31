using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A notification that is sent when an activity has executed.
/// </summary>
/// <remarks>
/// Also published on the throw path, before ExceptionHandlingMiddleware records the fault.
/// At that point <see cref="ActivityExecutionContext.Status"/> is still <see cref="ActivityStatus.Running"/>.
/// Subscribe to <see cref="ActivityFaulted"/> for the exception and the fact that the activity faulted.
/// </remarks>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
public record ActivityExecuted(ActivityExecutionContext ActivityExecutionContext) : INotification;
