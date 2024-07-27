using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class BookmarkUpdater(IBookmarkManager bookmarkManager, IBookmarkStore bookmarkStore) : IBookmarkUpdater
{
    /// <inheritdoc />
    public async Task UpdateBookmarksAsync(UpdateBookmarksRequest request, CancellationToken cancellationToken = default)
    {
        var instanceId = request.WorkflowInstanceId;
        await RemoveBookmarksAsync(instanceId, request.Diff.Removed, cancellationToken);
        await StoreBookmarksAsync(instanceId, request.Diff.Added, request.CorrelationId, cancellationToken);
    }
    
    private async Task RemoveBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var matchingHashes = bookmarks.Select(x => x.Hash).ToList();
        var filter = new BookmarkFilter { Hashes = matchingHashes, WorkflowInstanceId = workflowInstanceId };
        await bookmarkManager.DeleteManyAsync(filter, cancellationToken);
    }
    
    private async Task StoreBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, string? correlationId, CancellationToken cancellationToken)
    {
        foreach (var bookmark in bookmarks)
        {
            var storedBookmark = new StoredBookmark
            {
                Id = bookmark.Id,
                ActivityTypeName = bookmark.Name,
                Hash = bookmark.Hash,
                WorkflowInstanceId = workflowInstanceId,
                CreatedAt = bookmark.CreatedAt,
                ActivityInstanceId = bookmark.ActivityInstanceId,
                CorrelationId = correlationId,
                Payload = bookmark.Payload,
                Metadata = bookmark.Metadata
            };
            
            await bookmarkStore.SaveAsync(storedBookmark, cancellationToken);
        }
    }
}