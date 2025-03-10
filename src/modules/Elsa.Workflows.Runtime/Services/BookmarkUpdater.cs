using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class BookmarkUpdater(IBookmarkManager bookmarkManager, IBookmarkStore bookmarkStore) : IBookmarkUpdater
{
    /// <inheritdoc />
    public async Task UpdateBookmarksAsync(UpdateBookmarksRequest request, CancellationToken cancellationToken = default)
    {
        var instanceId = request.WorkflowExecutionContext.Id;
        await RemoveBookmarksAsync(instanceId, request.Diff.Removed.ToList(), cancellationToken);
        await StoreBookmarksAsync(request.WorkflowExecutionContext, request.Diff.Added.ToList(), cancellationToken);
    }

    private async Task RemoveBookmarksAsync(string workflowInstanceId, ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        if (bookmarks.Count == 0)
            return;

        var matchingIds = bookmarks.Select(x => x.Id).ToList();
        var filter = new BookmarkFilter
        {
            BookmarkIds = matchingIds,
            WorkflowInstanceId = workflowInstanceId
        };
        await bookmarkManager.DeleteManyAsync(filter, cancellationToken);
    }

    private async Task StoreBookmarksAsync(WorkflowExecutionContext context, ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        if (bookmarks.Count == 0)
            return;

        foreach (var bookmark in bookmarks)
        {
            var storedBookmark = context.MapBookmark(bookmark);
            await bookmarkStore.SaveAsync(storedBookmark, cancellationToken);
        }
    }
}