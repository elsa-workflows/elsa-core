using Elsa.Workflows.Runtime.Models;

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
    public ICollection<string> ActivityIds { get; set; } = default!;
}

internal class Response
{
    public ICollection<ActivityExecutionStats> Stats { get; set; } = new List<ActivityExecutionStats>();
}