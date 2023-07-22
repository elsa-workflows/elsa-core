using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Refit;

namespace Elsa.Api.Client.Resources.ActivityExecutions.Contracts;

/// <summary>
/// Represents a client for the activity executions API.
/// </summary>
public interface IActivityExecutionsApi
{
    /// <summary>
    /// Sends the specified request to the API.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Post("/activity-executions/report")]
    Task<ActivityExecutionReport> GetReportAsync([Body] GetActivityExecutionReportRequest request, CancellationToken cancellationToken = default);
}