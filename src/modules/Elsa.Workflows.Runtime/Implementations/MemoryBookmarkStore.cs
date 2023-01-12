using System.Collections.Concurrent;
using Elsa.Common.Implementations;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <inheritdoc />
public class MemoryBookmarkStore : IBookmarkStore
{
    private readonly MemoryStore<StoredBookmark> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="store"></param>
    public MemoryBookmarkStore(MemoryStore<StoredBookmark> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.BookmarkId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.FindMany(x => x.WorkflowInstanceId == workflowInstanceId).ToList();
        return new(bookmarks);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.FindMany(x => x.Hash == hash).ToList(); 
        return new(bookmarks);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAndHashAsync(string workflowInstanceId, string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.FindMany(x => x.WorkflowInstanceId == workflowInstanceId && x.Hash == hash).ToList();
        return new(bookmarks);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindByCorrelationAndHashAsync(string correlationId, string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.FindMany(x => x.CorrelationId == correlationId && x.Hash == hash).ToList();
        return new(bookmarks);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredBookmark>> FindByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default)
    {
        var bookmarks = _store.FindMany(x => x.ActivityTypeName == activityType).ToList();
        return new(bookmarks);
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        _store.DeleteWhere(x => x.Hash == hash && x.WorkflowInstanceId == workflowInstanceId);
        return ValueTask.CompletedTask;
    }
}