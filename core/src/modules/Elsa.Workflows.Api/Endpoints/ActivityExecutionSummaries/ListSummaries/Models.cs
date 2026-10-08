namespace Elsa.Workflows.Api.Endpoints.ActivityExecutionSummaries.ListSummaries;

/// <summary>
/// A request for a list of activity execution log record summaries for a given activity in a workflow instance.
/// </summary>
internal class Request
{
    /// <summary>
    /// The ID of the workflow instance to get the execution log for.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;

    /// <summary>
    /// The node ID of the activity to get the execution record for.
    /// </summary>
    public string ActivityNodeId { get; set; } = default!;
    
    /// <summary>
    /// Whether to include completed activity execution records. If not specified, all activity execution records will be included.
    /// </summary>
    public bool? Completed { get; set; }
}