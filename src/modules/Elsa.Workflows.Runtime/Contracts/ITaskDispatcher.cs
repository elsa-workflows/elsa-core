using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Dispatches a request for running a task.
/// </summary>
public interface ITaskDispatcher
{
    /// <summary>
    /// Asynchronously publishes the specified event using the workflow dispatcher.
    /// </summary>
    Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default);
}