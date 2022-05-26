using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Persistence.Services;

public interface IWorkflowTriggerStore
{
    Task SaveAsync(WorkflowTrigger record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowTrigger> records, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowTrigger>> FindManyByNameAsync(string name, string? hash, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowTrigger>> FindManyByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    Task ReplaceAsync(IEnumerable<WorkflowTrigger> removed, IEnumerable<WorkflowTrigger> added, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}