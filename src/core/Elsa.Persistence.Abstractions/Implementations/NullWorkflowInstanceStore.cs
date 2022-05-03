using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullWorkflowInstanceStore : IWorkflowInstanceStore
{
    public Task<WorkflowInstance?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<WorkflowInstance?>(default);
    public Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args, CancellationToken cancellationToken = default)
    {
        var pagedList = Page.Of(new List<WorkflowInstanceSummary>(0), 0);
        return Task.FromResult(pagedList);
    }
}