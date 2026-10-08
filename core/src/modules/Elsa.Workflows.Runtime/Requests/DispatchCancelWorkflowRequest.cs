namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// A dispatch request that indicates that one or more workflows are requested to be cancelled.
/// </summary>
public class DispatchCancelWorkflowRequest
{
    /// <summary>
    /// The ID of the workflow instance to cancel.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
}