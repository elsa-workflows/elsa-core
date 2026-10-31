using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A notification that is sent when an activity faults, either by throwing or by calling <c>Fault()</c>.
/// </summary>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
/// <param name="Exception">The exception that caused the fault. Carried on the notification because, on the throw path, the context is not faulted yet.</param>
public record ActivityFaulted(ActivityExecutionContext ActivityExecutionContext, Exception Exception) : INotification;
