using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A domain event that applications can subscribe to in order to handle tasks requested by a workflow.
/// </summary>
/// <param>name="ActivityExecutionContext">The context of the activity that requested the task to run.</param>
/// <param name="TaskId">A unique identifier for an individual task request.</param>
/// <param name="TaskName">The name of the task requested to run.</param>
/// <param name="TaskPayload">Any additional parameters to send to the task.</param>
public record RunTaskRequest(ActivityExecutionContext ActivityExecutionContext, string TaskId, string TaskName, IDictionary<string, object>? TaskPayload) : INotification;