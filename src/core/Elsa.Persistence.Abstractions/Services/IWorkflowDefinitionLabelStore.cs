using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Services;

public interface IWorkflowDefinitionLabelStore
{
    Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of label IDs associated with the specified workflow definition version ID.
    /// </summary>
    Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and adds the specified sets of label IDs for the specified workflow definition version ID.
    /// </summary>
    Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default);
}