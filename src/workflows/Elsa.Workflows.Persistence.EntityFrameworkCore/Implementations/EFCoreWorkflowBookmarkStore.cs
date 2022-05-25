using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowBookmarkStore : IWorkflowBookmarkStore
{
    private readonly IStore<ElsaDbContext, WorkflowBookmark> _store;

    public EFCoreWorkflowBookmarkStore(IStore<ElsaDbContext, WorkflowBookmark> store)
    {
        _store = store;
    }

    public async Task SaveAsync(WorkflowBookmark record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowBookmark> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);
    public async Task<WorkflowBookmark?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => await _store.FindAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<WorkflowBookmark>> FindManyAsync(string name, string? hash, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query =>
        {
            query = query.Where(x => x.Name == name);

            if (!string.IsNullOrWhiteSpace(hash))
                query = query.Where(x => x.Hash == hash);

            return query;
        }, cancellationToken);

    public async Task<IEnumerable<WorkflowBookmark>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.WorkflowInstanceId == workflowInstanceId, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }
}