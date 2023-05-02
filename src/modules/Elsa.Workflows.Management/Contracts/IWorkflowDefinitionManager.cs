using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Provides operations for managing workflow definitions.
/// </summary>
public interface IWorkflowDefinitionManager
{
    /// <summary>
    /// Deletes all workflow definition versions with the specified definition ID.
    /// </summary>
    /// <param name="definitionId">The definition ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted workflow definitions.</returns>
    Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Deletes a specific workflow definition version.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition.</param>
    /// <param name="version">The version number of the workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the workflow definition version was deleted, otherwise false.</returns>
    Task<bool> DeleteVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all workflow definition versions with the specified definition IDs.
    /// </summary>
    /// <param name="definitionIds">The definition IDs.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted workflow definitions.</returns>
    Task<int> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new workflow definition from the specified version.
    /// </summary>
    /// <param name="definitionId">The definition ID.</param>
    /// <param name="version">The version number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new workflow definition.</returns>
    Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates all referencing workflow definitions to use the version of the specified workflow definition.
    /// </summary>
    /// <param name="dependency">The workflow definition to update references for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated workflow definitions.</returns>
    Task<IEnumerable<WorkflowDefinition>> UpdateReferencesInConsumingWorkflows(WorkflowDefinition dependency, CancellationToken cancellationToken = default);
}