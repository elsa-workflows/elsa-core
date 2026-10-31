using Elsa.Mediator.Contracts;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A notification that is sent when an activity faults, either by throwing or by calling <c>Fault()</c>.
/// </summary>
/// <remarks>
/// This is not necessarily terminal. An enclosing container or incident strategy may still handle the fault
/// (for example via <see cref="FaultSignal"/>), after which the activity may no longer end as Faulted.
/// </remarks>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
/// <param name="Exception">The exception that caused the fault. Carried on the notification because, on the throw path, the context is not faulted yet.</param>
public record ActivityFaulted(ActivityExecutionContext ActivityExecutionContext, Exception Exception) : INotification;
