using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class BookmarksPersister(IBookmarkUpdater bookmarkUpdater, INotificationSender notificationSender) : IBookmarksPersister
{
    /// <inheritdoc />
    public async Task PersistBookmarksAsync(UpdateBookmarksRequest updateBookmarksRequest)
    {
        await bookmarkUpdater.UpdateBookmarksAsync(updateBookmarksRequest);

        // Publish domain event.
        await notificationSender.SendAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(
            updateBookmarksRequest.WorkflowExecutionContext,
            updateBookmarksRequest.Diff.Added, 
            updateBookmarksRequest.Diff.Removed, 
            updateBookmarksRequest.Diff.Unchanged)));

        // Publish domain event.
        await notificationSender.SendAsync(new WorkflowBookmarksPersisted(updateBookmarksRequest.Diff));
    }
}