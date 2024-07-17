using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.HasUpdates;

/// The request to check if there are updates for a workflow instance journal.
public class Request
{
    /// The unique identifier of a workflow instance.
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;
}

/// Represents the response containing the last updated timestamp of a workflow instance.
public record Response(DateTimeOffset UpdatedAt);