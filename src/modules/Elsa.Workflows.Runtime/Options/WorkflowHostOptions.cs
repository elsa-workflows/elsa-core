namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Represents the options for creating a workflow host.
/// </summary>
public class WorkflowHostOptions
{
    /// <summary>
    /// Gets or sets the ID of the workflow instance to use when creating the workflow host and its workflow state.
    /// </summary>
    public string? NewWorkflowInstanceId { get; set; }
}