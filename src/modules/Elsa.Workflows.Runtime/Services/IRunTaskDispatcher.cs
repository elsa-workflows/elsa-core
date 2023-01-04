using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Dispatches a request for running a task.
/// </summary>
public interface IRunTaskDispatcher
{
    /// <summary>
    /// Asynchronously publishes the specified event using the workflow dispatcher.
    /// </summary>
    Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default);
}