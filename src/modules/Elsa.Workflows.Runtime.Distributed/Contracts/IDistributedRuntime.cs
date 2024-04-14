namespace Elsa.Workflows.Runtime.Distributed.Contracts;

/// <summary>
/// Represents a distributed runtime that can create <see cref="IWorkflowClient"/> instances connected to a workflow instance in a distributed environment.
/// </summary>
public interface IDistributedRuntime
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to create a client.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    /// <remarks>The workflow instance itself doesn't have to exist yet.</remarks>
    ValueTask<IWorkflowClient> CreateClientAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
}