namespace Elsa.Workflows.Management.Exceptions;

/// <summary>
/// A new workflow could not reserve the requested instance identifier because it is already in use.
/// </summary>
public class WorkflowInstanceAlreadyExistsException(string workflowInstanceId, Exception? innerException = null)
    : InvalidOperationException("The workflow instance identifier is already in use.", innerException)
{
    public string WorkflowInstanceId { get; } = workflowInstanceId;
}
