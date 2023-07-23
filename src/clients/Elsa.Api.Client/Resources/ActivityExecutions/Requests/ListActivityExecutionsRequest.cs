using Refit;

namespace Elsa.Api.Client.Resources.ActivityExecutions.Requests;

/// <summary>
/// Represents a request for a list of activity execution records for a given activity in a workflow instance.
/// </summary>
public class ListActivityExecutionsRequest
{
    /// <summary>
    /// The ID of the workflow instance to get the execution log for.
    /// </summary>
    [Query]
    public string WorkflowInstanceId { get; set; } = default!;

    /// <summary>
    /// The ID of the activity to get the execution record for.
    /// </summary>
    [Query]
    public string ActivityId { get; set; } = default!;
}