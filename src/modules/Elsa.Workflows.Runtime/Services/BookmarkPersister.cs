using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class BookmarksPersister(IBookmarkUpdater bookmarkUpdater, INotificationSender notificationSender) : IBookmarksPersister
{
    /// <inheritdoc />
    public async Task PersistBookmarksAsync(UpdateBookmarksRequest updateBookmarksRequest)
    {
        await bookmarkUpdater.UpdateBookmarksAsync(updateBookmarksRequest);
    
        // Publish domain event.
        await notificationSender.SendAsync(new WorkflowBookmarksIndexed(updateBookmarksRequest.WorkflowExecutionContext, new IndexedWorkflowBookmarks(updateBookmarksRequest.WorkflowInstanceId, updateBookmarksRequest.Diff.Added, updateBookmarksRequest.Diff.Removed, updateBookmarksRequest.Diff.Unchanged)));
    
        // Publish domain event.
        await notificationSender.SendAsync(new WorkflowBookmarksPersisted(updateBookmarksRequest.Diff), NotificationStrategy.Background);
    }
}