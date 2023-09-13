using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Publishes events using the workflow runtime, effectively triggering all <see cref="Event"/> activities.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Synchronously publishes the specified event using the workflow runtime, effectively triggering all <see cref="Event"/> activities matching the provided event name.
    /// </summary>
    Task<ICollection<WorkflowExecutionResult>> PublishAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, string? activityInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously publishes the specified event using the workflow dispatcher.
    /// </summary>
    Task DispatchAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, string? activityInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
}