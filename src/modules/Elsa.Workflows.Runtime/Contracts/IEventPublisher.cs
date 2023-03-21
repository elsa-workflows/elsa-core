using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Publishes events using the workflow runtime, effectively triggering all <see cref="Event"/> activities.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Synchronously publishes the specified event using the workflow runtime, effectively triggering all <see cref="Event"/> activities matching the provided event name.
    /// </summary>
    Task PublishAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously publishes the specified event using the workflow dispatcher.
    /// </summary>
    Task DispatchAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
}