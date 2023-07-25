using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// A service for reading activity execution logs.
/// </summary>
public interface IActivityExecutionService
{
    /// <summary>
    /// Gets execution stats for the specified activities.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <param name="activityIds">The activity IDs.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The activity execution stats.</returns>
    Task<IEnumerable<ActivityExecutionStats>> GetStatsAsync(string workflowInstanceId, IEnumerable<string> activityIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets execution stats for the specified activity.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The activity execution stats.</returns>
    Task<ActivityExecutionStats> GetStatsAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default);
}