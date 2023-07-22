namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Report;

/// <summary>
/// Represents a request for a list of activity execution log records for a given activity in a workflow instance.
/// </summary>
internal class Request
{
    /// <summary>
    /// The ID of the workflow instance to get the execution log for.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;

    /// <summary>
    /// The ID of the activity to get the execution record for.
    /// </summary>
    public ICollection<string> ActivityIds { get; set; } = new List<string>();
}

internal class Response
{
    public ICollection<ActivityExecutionStats> Stats { get; set; } = new List<ActivityExecutionStats>();
}

internal class ActivityExecutionStats
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