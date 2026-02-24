using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.ActivityExecutions.Contracts;

/// <summary>
/// Represents a client for the activity executions API.
/// </summary>
public interface IActivityExecutionsApi
{
    /// <summary>
    /// Gets a report of activity executions for a given workflow instance.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The response containing the activity execution report.</returns>
    [Post("/activity-executions/report")]
    Task<ActivityExecutionReport> GetReportAsync([Body] GetActivityExecutionReportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists activity executions for a given activity in a workflow instance.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The response containing a list of activity executions.</returns>
    [Get("/activity-executions/list")]
    Task<ListResponse<ActivityExecutionRecord>> ListAsync(ListActivityExecutionsRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists activity execution summaries for a given activity in a workflow instance.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The response containing a list of activity execution summaries.</returns>
    [Get("/activity-execution-summaries/list")]
    Task<ListResponse<ActivityExecutionRecordSummary>> ListSummariesAsync(ListActivityExecutionsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single activity execution by ID.
    /// </summary>
    /// <param name="id">The ID of the activity execution.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The activity execution.</returns>
    [Get("/activity-executions/{id}")]
    Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the call stack (execution chain) for a given activity execution.
    /// </summary>
    /// <param name="id">The ID of the activity execution.</param>
    /// <param name="includeCrossWorkflowChain">Whether to include parent workflow activities across workflow boundaries.</param>
    /// <param name="skip">The number of items to skip (for pagination).</param>
    /// <param name="take">The maximum number of items to return (for pagination).</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The response containing the call stack.</returns>
    [Get("/activity-executions/{id}/call-stack")]
    Task<ActivityExecutionCallStack> GetCallStackAsync(string id, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}
