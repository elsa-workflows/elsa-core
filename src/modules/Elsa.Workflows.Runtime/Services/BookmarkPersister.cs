using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class BookmarksPersister(IBookmarkUpdater bookmarkUpdater, INotificationSender notificationSender) : IBookmarksPersister
{
    
    
    /// <inheritdoc />
    public async Task PersistBookmarksAsync(WorkflowExecutionContext context, Diff<Bookmark> diff)
    {
        var cancellationToken = context.CancellationTokens.SystemCancellationToken;
        var updateBookmarksContext = new UpdateBookmarksRequest(context.Id, diff, context.CorrelationId);
        await bookmarkUpdater.UpdateBookmarksAsync(updateBookmarksContext, cancellationToken);

        // Publish domain event.
        await notificationSender.SendAsync(new WorkflowBookmarksIndexed(context, new IndexedWorkflowBookmarks(context.Id, diff.Added, diff.Removed, diff.Unchanged)), cancellationToken);

        // Notify all interested activities that the bookmarks have been persisted.
        var activityExecutionContexts = context.ActivityExecutionContexts.Where(x => x.Activity is IBookmarksPersistedHandler && x.Bookmarks.Any()).ToList();

        foreach (var activityExecutionContext in activityExecutionContexts) 
            await ((IBookmarksPersistedHandler)activityExecutionContext.Activity).BookmarksPersistedAsync(activityExecutionContext);
        
        // Publish domain event.
        await notificationSender.SendAsync(new WorkflowBookmarksPersisted(context, diff), NotificationStrategy.Background, cancellationToken);
    }
}