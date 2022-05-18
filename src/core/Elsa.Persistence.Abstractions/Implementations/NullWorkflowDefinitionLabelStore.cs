using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore
{
    public Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Task.FromResult(0);
}