using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Persistence.Services;

public interface IWorkflowBookmarkStore
{
    Task SaveAsync(WorkflowBookmark record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowBookmark> records, CancellationToken cancellationToken = default);
    Task<WorkflowBookmark?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowBookmark>> FindManyAsync(string name, string? hash = default, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowBookmark>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}