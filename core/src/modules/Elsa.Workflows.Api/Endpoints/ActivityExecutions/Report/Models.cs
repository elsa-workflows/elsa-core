using Elsa.Workflows.Runtime;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Report;

/// <summary>
/// Represents a request for a list of activity execution log records for a given activity in a workflow instance.
/// </summary>
internal class Request
{
    /// <summary>
    /// The ID of the workflow instance to get the execution log for.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = null!;

    /// <summary>
    /// The node IDs of the activities to get the execution records for.
    /// </summary>
    public ICollection<string> ActivityNodeIds { get; set; } = null!;
}

internal class Response
{
    public ICollection<ActivityExecutionStats> Stats { get; set; } = new List<ActivityExecutionStats>();
}