using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of loading and persisting bookmarks.
/// </summary>
public class PersistBookmarkMiddleware : WorkflowExecutionMiddleware
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly INotificationSender _notificationSender;

    /// <inheritdoc />
    public PersistBookmarkMiddleware(WorkflowMiddlewareDelegate next, IWorkflowRuntime workflowRuntime, INotificationSender notificationSender) : base(next)
    {
        _workflowRuntime = workflowRuntime;
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var cancellationToken = context.CancellationTokens.SystemCancellationToken;

        // Get current bookmarks.
        var originalBookmarks = context.Bookmarks.ToList();

        // Invoke next middleware.
        await Next(context);

        // Get new bookmarks.
        var updatedBookmarks = context.Bookmarks.ToList();

        // Get a diff.
        var diff = Diff.For(originalBookmarks, updatedBookmarks);

        // Update bookmarks.
        var updateBookmarksContext = new UpdateBookmarksRequest(context, diff, context.CorrelationId);
        await _workflowRuntime.UpdateBookmarksAsync(updateBookmarksContext, cancellationToken);

        // Publish domain event.
        await _notificationSender.SendAsync(new WorkflowBookmarksIndexed(context, new IndexedWorkflowBookmarks(context.Id, diff.Added, diff.Removed, diff.Unchanged)), cancellationToken);

        // Notify all interested activities that the bookmarks have been persisted.
        var activityExecutionContexts = context.ActivityExecutionContexts.Where(x => x.Activity is IBookmarksPersistedHandler && x.Bookmarks.Any()).ToList();

        foreach (var activityExecutionContext in activityExecutionContexts) 
            await ((IBookmarksPersistedHandler)activityExecutionContext.Activity).BookmarksPersistedAsync(activityExecutionContext);
        
        // Publish domain event.
        await _notificationSender.SendAsync(new WorkflowBookmarksPersisted(context, diff), NotificationStrategy.Background, cancellationToken);
    }
}