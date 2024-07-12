using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.HasUpdates;

/// The request to check if there are updates for a workflow instance journal.
public class Request
{
    /// The unique identifier of a workflow instance.
    [BindFrom("id")] public string WorkflowInstanceId { get; set; } = default!;

    /// The start date for checking for updates in the workflow instance journal.
    public DateTimeOffset UpdatesSince { get; set; }
}