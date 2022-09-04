using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

public class EFCoreBookmarkStore : IBookmarkStore
{
    private readonly Store<RuntimeDbContext, StoredBookmark> _store;
    public EFCoreBookmarkStore(Store<RuntimeDbContext, StoredBookmark> store) => _store = store;

    public async ValueTask SaveAsync(string activityTypeName, string hash, string workflowInstanceId, IEnumerable<string> bookmarkIds, CancellationToken cancellationToken = default)
    {
        var storedBookmarks = bookmarkIds.Select(x => new StoredBookmark(activityTypeName, hash, workflowInstanceId, x)).ToList();
        await _store.SaveManyAsync(storedBookmarks, cancellationToken);
    }

    public async ValueTask<IEnumerable<StoredBookmark>> LoadAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => 
        await _store.FindManyAsync(x => x.WorkflowInstanceId == workflowInstanceId, cancellationToken);

    public async ValueTask<IEnumerable<StoredBookmark>> LoadAsync(string activityTypeName, string hash, CancellationToken cancellationToken = default) => 
        await _store.FindManyAsync(x => x.ActivityTypeName == activityTypeName && x.Hash == hash, cancellationToken);

    public async ValueTask DeleteAsync(string activityTypeName, string hash, string workflowInstanceId, CancellationToken cancellationToken = default) => 
        await _store.DeleteWhereAsync(x => x.WorkflowInstanceId == workflowInstanceId && x.ActivityTypeName == activityTypeName && x.Hash == hash, cancellationToken);
}