using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.ExecutionState;

/// <summary>
/// The request to check the execution state of the workflow instance.
/// </summary>
public class Request
{
    /// <summary>
    /// The unique identifier of a workflow instance.
    /// </summary>
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;
}

/// <summary>
/// Represents the response containing the last updated timestamp of a workflow instance.
/// </summary>
public record Response(WorkflowStatus Status, WorkflowSubStatus SubStatus, DateTimeOffset UpdatedAt);