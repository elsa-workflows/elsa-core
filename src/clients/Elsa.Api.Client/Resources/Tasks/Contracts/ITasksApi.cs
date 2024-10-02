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
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Post("/tasks/{taskId}/complete")]
    Task ReportTaskCompletedRequestAsync([Body] ReportTaskCompletedRequest request, CancellationToken cancellationToken = default);
}