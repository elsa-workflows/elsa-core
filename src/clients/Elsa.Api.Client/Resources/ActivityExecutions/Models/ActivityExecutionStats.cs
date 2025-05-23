namespace Elsa.Api.Client.Resources.ActivityExecutions.Models;

/// <summary>
/// Represents statistics about the execution of an activity.
/// </summary>
public class ActivityExecutionStats
{
    /// <summary>
    /// Gets or sets the ID of the activity.
    /// </summary>
    public string ActivityId { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the node ID of the activity.
    /// </summary>
    public string ActivityNodeId { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the number of uncompleted executions.
    /// </summary>
    public long StartedCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of completed executions.
    /// </summary>
    public long CompletedCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of uncompleted executions.
    /// </summary>
    public long UncompletedCount { get; set; }

    /// <summary>
    /// Gets or sets a value whether this activity is waiting for bookmarks to be resumed.
    /// </summary>
    public bool IsBlocked { get; set; }
    
    /// <summary>
    /// Gets or sets a value whether this activity is faulted.
    /// </summary>
    public bool IsFaulted { get; set; }

    /// <summary>
    /// Gets or sets the total count of faults aggregated from the activity execution and its descendants.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
}