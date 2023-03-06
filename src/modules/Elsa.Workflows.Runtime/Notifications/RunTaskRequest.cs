using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A domain event that applications can subscribe to in order to handle tasks requested by a workflow.
/// </summary>
/// <param name="TaskId">A unique identifier for an individual task request.</param>
/// <param name="TaskName">The name of the task requested to run.</param>
/// <param name="TaskParams">AnyAsync parameters supplied by the requester of the task.</param>
public record RunTaskRequest(string TaskId, string TaskName, object? TaskParams) : INotification;