using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// A notification that is sent when an activity is about to execute.
/// </summary>
/// <param name="ActivityExecutionContext">The activity execution context.</param>
public record ActivityExecuting(ActivityExecutionContext ActivityExecutionContext) : INotification;