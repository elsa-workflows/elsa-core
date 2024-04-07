using System.Diagnostics.CodeAnalysis;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a factory that creates <see cref="IWorkflowClient"/> instances.
/// </summary>
public interface IWorkflowClientFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="clientType">The type of the client to create.</param>
    /// <param name="workflowDefinitionVersionId">The ID of the workflow definition version for which to create a client.</param>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to create a client. If not specified, a new workflow instance will be created.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    Task<IWorkflowClient> CreateClientAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type clientType,
        string workflowDefinitionVersionId,
        string? workflowInstanceId = null,
        CancellationToken cancellationToken = default);
}