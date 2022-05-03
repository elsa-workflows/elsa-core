using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullWorkflowBookmarkStore : IWorkflowBookmarkStore
{
    public Task SaveAsync(WorkflowBookmark record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<WorkflowBookmark> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<WorkflowBookmark?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<WorkflowBookmark?>(default);
    public Task<IEnumerable<WorkflowBookmark>> FindManyAsync(string name, string? hash, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<WorkflowBookmark>());
    public Task<IEnumerable<WorkflowBookmark>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<WorkflowBookmark>());
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => Task.FromResult(0);
}