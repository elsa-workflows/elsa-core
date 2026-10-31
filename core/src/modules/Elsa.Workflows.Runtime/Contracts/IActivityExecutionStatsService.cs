namespace Elsa.Workflows.Runtime;

/// <summary>
/// A service for reading activity execution logs.
/// </summary>
public interface IActivityExecutionStatsService
{
    /// <summary>
    /// Gets execution stats for the specified activities.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <param name="activityNodeIds">The activity node IDs.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The activity execution stats.</returns>
    Task<IEnumerable<ActivityExecutionStats>> GetStatsAsync(string workflowInstanceId, IEnumerable<string> activityNodeIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets execution stats for the specified activity.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <param name="activityNodeId">The activity node ID.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The activity execution stats.</returns>
    Task<ActivityExecutionStats> GetStatsAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default);
}