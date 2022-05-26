using Elsa.Persistence.Common.Implementations;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.Implementations;

public class MemoryWorkflowBookmarkStore : IWorkflowBookmarkStore
{
    private readonly MemoryStore<WorkflowBookmark> _store;

    public MemoryWorkflowBookmarkStore(MemoryStore<WorkflowBookmark> store)
    {
        _store = store;
    }
    
    public Task SaveAsync(WorkflowBookmark record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowBookmark> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records);
        return Task.CompletedTask;
    }

    public Task<WorkflowBookmark?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var bookmark = _store.Find(x => x.Id == id);
        return Task.FromResult(bookmark);
    }

    public Task<IEnumerable<WorkflowBookmark>> FindManyAsync(string name, string? hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.Query(query =>
        {
            query = query.Where(x => x.Name == name);

            if (!string.IsNullOrWhiteSpace(hash))
                query = query.Where(x => x.Hash == hash);

            return query;
        });

        return Task.FromResult(bookmarks);
    }

    public Task<IEnumerable<WorkflowBookmark>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.FindMany(x => x.WorkflowInstanceId == workflowInstanceId);
        return Task.FromResult(bookmarks);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = _store.Delete(id);
        return Task.FromResult(result);
    }

    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteMany(ids);
        return Task.FromResult(result);
    }
}