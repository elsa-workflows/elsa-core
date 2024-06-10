using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IBookmarkManager"/>.
/// </summary>
public class DefaultBookmarkManager(IBookmarkStore bookmarkStore, INotificationSender notificationSender, ILogger<DefaultBookmarkManager> logger)
    : IBookmarkManager
{
    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var bookmarks = (await bookmarkStore.FindManyAsync(filter, cancellationToken)).ToList();
        await notificationSender.SendAsync(new BookmarksDeleting(bookmarks), cancellationToken);
        var count = await bookmarkStore.DeleteAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new BookmarksDeleted(bookmarks), cancellationToken);

        if (logger.IsEnabled(LogLevel.Debug))
            foreach (var bookmark in bookmarks)
                logger.LogDebug("Deleted bookmark {BookmarkId} for workflow {WorkflowInstanceId}", bookmark.BookmarkId, bookmark.WorkflowInstanceId);

        return count;
    }
}