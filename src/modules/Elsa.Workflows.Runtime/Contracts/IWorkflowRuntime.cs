namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a workflow runtime that can create <see cref="IWorkflowClient"/> instances connected to a workflow instance.
/// </summary>
public interface IWorkflowRuntime
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    /// <remarks>The workflow instance doesn't exist yet, and a new workflow instance ID will be generated.</remarks>
    ValueTask<IWorkflowClient> CreateClientAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to create a client. If <c>null</c>, a new workflow instance ID will be generated.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    /// <remarks>The workflow instance itself doesn't have to exist yet.</remarks>
    ValueTask<IWorkflowClient> CreateClientAsync(string? workflowInstanceId, CancellationToken cancellationToken = default);
}