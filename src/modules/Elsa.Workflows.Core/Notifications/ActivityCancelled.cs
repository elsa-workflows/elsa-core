using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A notification that is sent when an activity is cancelled.
/// </summary>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
public record ActivityCancelled(ActivityExecutionContext ActivityExecutionContext) : INotification;