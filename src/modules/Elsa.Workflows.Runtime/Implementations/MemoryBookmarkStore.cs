using System.Collections.Concurrent;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class MemoryBookmarkStore : IBookmarkStore
{
    private readonly ConcurrentDictionary<string, ICollection<StoredBookmark>> _bookmarks = new();

    public ValueTask SaveAsync(string activityTypeName, string hash, string workflowInstanceId, IEnumerable<string> bookmarkIds, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var storedBookmarks = bookmarkIds.Select(x => new StoredBookmark(activityTypeName, hash, workflowInstanceId, x, correlationId)).ToList();
        _bookmarks.AddOrUpdate(hash, new List<StoredBookmark>(storedBookmarks), (s, bookmarks) => bookmarks.Concat(storedBookmarks).ToList());
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarks.Values.SelectMany(x => x).Where(x => x.WorkflowInstanceId == workflowInstanceId).ToList();
        return new(bookmarks);
    }

    public ValueTask<IEnumerable<StoredBookmark>> FindByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarks.TryGetValue(hash, out var value) ? value : Enumerable.Empty<StoredBookmark>(); 
        return new(bookmarks);
    }

    public ValueTask<IEnumerable<StoredBookmark>> FindByWorkflowInstanceAndHashAsync(string workflowInstanceId, string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarks.Values.SelectMany(x => x).Where(x => x.WorkflowInstanceId == workflowInstanceId && x.Hash == hash).ToList();
        return new(bookmarks);
    }
    
    public ValueTask<IEnumerable<StoredBookmark>> FindByCorrelationAndHashAsync(string correlationId, string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarks.Values.SelectMany(x => x).Where(x => x.CorrelationId == correlationId && x.Hash == hash).ToList();
        return new(bookmarks);
    }

    public ValueTask DeleteAsync(string hash, string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var key = hash;
        
        if(!_bookmarks.TryGetValue(key, out var bookmarks))
            return ValueTask.CompletedTask;
        
        var updatedBookmarks = bookmarks.Where(x => x.WorkflowInstanceId != workflowInstanceId).ToList();

        if (!_bookmarks.TryUpdate(key, updatedBookmarks, bookmarks))
            throw new Exception("Failed to update bookmarks");
        
        return ValueTask.CompletedTask;
    }
}