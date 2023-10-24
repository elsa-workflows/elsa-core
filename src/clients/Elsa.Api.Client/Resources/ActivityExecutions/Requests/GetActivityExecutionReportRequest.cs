namespace Elsa.Api.Client.Resources.ActivityExecutions.Requests;

/// <summary>
/// Represents a request for a list of activity execution records for a given activity in a workflow instance.
/// </summary>
public class GetActivityExecutionReportRequest
{
    /// <summary>
    /// The ID of the workflow instance to get the execution log for.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;

    /// <summary>
    /// The node IDs of the activities to get the execution record for.
    /// </summary>
    public ICollection<string> ActivityNodeIds { get; set; } = default!;
}