namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

/// <summary>
/// Represents a request to bulk delete workflow instances.
/// </summary>
public class BulkDeleteWorkflowInstancesRequest
{
    /// <summary>
    /// Gets or sets the IDs of the workflow instances to delete.
    /// </summary>
    public ICollection<string> Ids { get; set; } = default!;
}