using Elsa.Api.Client.Resources.Tasks.Requests;
using Refit;

namespace Elsa.Api.Client.Resources.Tasks.Contracts;

/// <summary>
/// Represents a client for the tasks API.
/// </summary>
public interface ITasksApi
{
    /// <summary>
    /// Sends the specified request to the tasks API.
    /// </summary>
    /// <param name="taskId">The Id of the task being reported as completed.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Post("/tasks/{taskId}/complete")]
    Task ReportTaskCompletedAsync(string taskId, [Body] ReportTaskCompletedRequest request, CancellationToken cancellationToken = default);
}