namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Count;

/// <summary>
/// Represents a request for list of activity execution log records for a given activity in a workflow instance.
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
    public string ActivityId { get; set; } = default!;

    /// <summary>
    /// Whether to include completed activity execution records. If not specified, all activity execution records will be included.
    /// </summary>
    public bool? Completed { get; set; }
}