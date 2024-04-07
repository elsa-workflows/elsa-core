namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a factory that creates <see cref="IWorkflowClient"/> instances.
/// </summary>
public interface IWorkflowClientFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to create a client.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    IWorkflowClient CreateClient(string workflowInstanceId);
}