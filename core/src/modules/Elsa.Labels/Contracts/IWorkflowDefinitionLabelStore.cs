using Elsa.Labels.Entities;

namespace Elsa.Labels.Contracts;

public interface IWorkflowDefinitionLabelStore
{
    Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of label IDs associated with the specified workflow definition version ID.
    /// </summary>
    Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and adds the specified sets of label IDs for the specified workflow definition version ID.
    /// </summary>
    Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all associated labels for the specified workflow definition ID.
    /// </summary>
    Task<long> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all associated labels for the specified workflow definition version ID.
    /// </summary>
    Task<long> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all associated labels for the specified workflow definition IDs.
    /// </summary>
    Task<long> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all associated labels for the specified workflow definition version IDs.
    /// </summary>
    Task<long> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default);
}