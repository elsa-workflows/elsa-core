using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management;

/// <summary>
/// Builds a complete graph of workflow references by recursively discovering all workflows
/// that consume (directly or indirectly) a given workflow definition.
/// </summary>
public interface IWorkflowReferenceGraphBuilder
{
    /// <summary>
    /// Builds a complete reference graph starting from the specified workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to start from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="WorkflowReferenceGraph"/> containing all reference relationships.</returns>
    Task<WorkflowReferenceGraph> BuildGraphAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a complete reference graph starting from multiple workflow definitions.
    /// </summary>
    /// <param name="definitionIds">The IDs of the workflow definitions to start from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A merged <see cref="WorkflowReferenceGraph"/> containing all reference relationships from all starting points.</returns>
    Task<WorkflowReferenceGraph> BuildGraphAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
}

