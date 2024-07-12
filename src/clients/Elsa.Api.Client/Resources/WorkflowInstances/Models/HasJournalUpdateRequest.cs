namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// A request to update a journal for a workflow instance.
public class HasJournalUpdateRequest
{
    /// The unique identifier of a workflow instance.
    public string WorkflowInstanceId { get; set; } = default!;

    /// The start date for checking for updates in the workflow instance journal.
    public DateTimeOffset UpdatesSince { get; set; }
}