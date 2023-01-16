using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

public class EFCoreBookmarkStore : IBookmarkStore
{
    private readonly Store<RuntimeElsaDbContext, StoredBookmark> _store;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreBookmarkStore(Store<RuntimeElsaDbContext, StoredBookmark> store) => _store = store;

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, s => s.Hash, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.WorkflowInstanceId == workflowInstanceId, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.Hash == hash, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAndHashAsync(string workflowInstanceId, string hash, CancellationToken cancellationToken) => 
        await _store.FindManyAsync(x => x.WorkflowInstanceId == workflowInstanceId && x.Hash == hash, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindByCorrelationAndHashAsync(string correlationId, string hash, CancellationToken cancellationToken) => 
        await _store.FindManyAsync(x => x.CorrelationId == correlationId && x.Hash == hash, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) => 
        await _store.FindManyAsync(x => x.ActivityTypeName == activityType, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.WorkflowInstanceId == workflowInstanceId && x.Hash == hash, cancellationToken);
}