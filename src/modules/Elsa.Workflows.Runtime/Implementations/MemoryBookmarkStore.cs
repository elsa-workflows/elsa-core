using System.Collections.Concurrent;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class MemoryBookmarkStore : IBookmarkStore
{
    private readonly ConcurrentDictionary<(string ActivityTypeName, string Hash), ICollection<StoredBookmark>> _bookmarks = new();

    public ValueTask SaveAsync(string activityTypeName, string hash, string workflowInstanceId, IEnumerable<string> bookmarkIds, CancellationToken cancellationToken = default)
    {
        var storedBookmarks = bookmarkIds.Select(x => new StoredBookmark(activityTypeName, hash, workflowInstanceId, x)).ToList();
        _bookmarks.AddOrUpdate((activityTypeName, hash), new List<StoredBookmark>(storedBookmarks), (s, bookmarks) => bookmarks.Concat(storedBookmarks).ToList());
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<StoredBookmark>> FindAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarks.Values.SelectMany(x => x).Where(x => x.WorkflowInstanceId == workflowInstanceId).ToList();
        return new(bookmarks);
    }

    public ValueTask<IEnumerable<StoredBookmark>> FindAsync(string activityTypeName, string hash, CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarks.TryGetValue((activityTypeName, hash), out var value) ? value : Enumerable.Empty<StoredBookmark>(); 
        return new(bookmarks);
    }

    public ValueTask DeleteAsync(string activityTypeName, string hash, string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var key = (activityTypeName, hash);
        
        if(!_bookmarks.TryGetValue(key, out var bookmarks))
            return ValueTask.CompletedTask;
        
        var updatedBookmarks = bookmarks.Where(x => x.WorkflowInstanceId != workflowInstanceId).ToList();

        if (!_bookmarks.TryUpdate(key, updatedBookmarks, bookmarks))
            throw new Exception("Failed to update bookmarks");
        
        return ValueTask.CompletedTask;
    }
}