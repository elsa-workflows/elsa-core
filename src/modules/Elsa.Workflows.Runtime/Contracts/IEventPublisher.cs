using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Publishes events using the workflow runtime, effectively triggering all <see cref="Event"/> activities.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes the specified event.
    /// </summary>
    Task PublishAsync(string eventName, string? correlationId = null, string? workflowInstanceId = null, string? activityInstanceId = null, object? payload = null, bool asynchronous = false, CancellationToken cancellationToken = default);
}