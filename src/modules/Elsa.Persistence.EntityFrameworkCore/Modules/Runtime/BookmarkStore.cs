using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

public class EFCoreBookmarkStore : IBookmarkStore
{
    private readonly Store<RuntimeElsaDbContext, StoredBookmark> _store;
    public EFCoreBookmarkStore(Store<RuntimeElsaDbContext, StoredBookmark> store) => _store = store;

    public async ValueTask SaveAsync(
        string activityTypeName,
        string hash,
        string workflowInstanceId,
        IEnumerable<string> bookmarkIds,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var storedBookmarks = bookmarkIds.Select(x => new StoredBookmark(activityTypeName, hash, workflowInstanceId, x, correlationId)).ToList();
        await _store.SaveManyAsync(storedBookmarks, cancellationToken);
    }

    public async ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.WorkflowInstanceId == workflowInstanceId, cancellationToken);

    public async ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.Hash == hash, cancellationToken);

    public async ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAndHashAsync(string workflowInstanceId, string hash, CancellationToken cancellationToken) => 
        await _store.FindManyAsync(x => x.WorkflowInstanceId == workflowInstanceId && x.Hash == hash, cancellationToken);
    
    public async ValueTask<IEnumerable<StoredBookmark>> FindByCorrelationAndHashAsync(string correlationId, string hash, CancellationToken cancellationToken) => 
        await _store.FindManyAsync(x => x.CorrelationId == correlationId && x.Hash == hash, cancellationToken);

    public async ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.WorkflowInstanceId == workflowInstanceId && x.Hash == hash, cancellationToken);
}