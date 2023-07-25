namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Execution stats about a given activity.
/// </summary>
public class ActivityExecutionStats
{
    /// <summary>
    /// Gets or sets the ID of the activity.
    /// </summary>
    public string ActivityId { get; set; } = default!;
    
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
}