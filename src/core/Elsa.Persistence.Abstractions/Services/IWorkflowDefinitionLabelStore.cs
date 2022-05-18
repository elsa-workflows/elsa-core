using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Services;

public interface IWorkflowDefinitionLabelStore
{
    Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}