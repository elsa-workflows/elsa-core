namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// A response to a request to create a new workflow instance.
/// </summary>
public record CreateWorkflowInstanceResponse
{
    /// <summary>
    /// When <c>true</c>, the workflow's activation strategy refused to create a new instance.
    /// No instance was persisted.
    /// </summary>
    public bool CannotStart { get; init; }
}