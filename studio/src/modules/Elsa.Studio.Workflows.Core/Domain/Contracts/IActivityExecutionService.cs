using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that provides information about the execution of activities.
/// </summary>
public interface IActivityExecutionService
{
    /// <summary>
    /// Gets a report of the execution of activities.
    /// </summary>
    Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of activity execution records.
    /// </summary>
    Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of activity execution record summaries.
    /// </summary>
    Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an individual activity execution record.
    /// </summary>
    Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the call stack (execution chain) for a given activity execution.
    /// </summary>
    Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of retry attempt records for a specific activity instance.
    /// </summary>
    Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}
