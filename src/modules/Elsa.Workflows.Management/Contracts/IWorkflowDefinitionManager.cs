using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Contracts;

public interface IWorkflowDefinitionManager
{
    Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    Task<int> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinition>> UpdateReferencesInConsumingWorkflows(string definitionId, int version, CancellationToken cancellationToken = default);
}