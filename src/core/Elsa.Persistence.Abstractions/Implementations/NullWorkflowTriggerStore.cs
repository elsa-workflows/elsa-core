using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullWorkflowTriggerStore : IWorkflowTriggerStore
{
    public Task SaveAsync(WorkflowTrigger record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<WorkflowTrigger> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IEnumerable<WorkflowTrigger>> FindManyByNameAsync(string name, string? hash, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<WorkflowTrigger>());
    public Task<IEnumerable<WorkflowTrigger>> FindManyByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<WorkflowTrigger>());
    public Task ReplaceAsync(IEnumerable<WorkflowTrigger> removed, IEnumerable<WorkflowTrigger> added, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
}