using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.ExecutionState;

/// The request to check the execution state of the workflow instance. 
public class Request
{
    /// The unique identifier of a workflow instance.
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;
}

/// Represents the response containing the last updated timestamp of a workflow instance.
public record Response(WorkflowStatus Status, WorkflowSubStatus SubStatus, DateTimeOffset UpdatedAt);