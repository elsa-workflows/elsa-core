using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Kafka.Handlers;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
[UsedImplicitly]
public class UpdateWorkers(IWorkerManager workerManager) :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>,
    INotificationHandler<BookmarksDeleted>
{
    /// Adds, updates and removes workers based on added and removed triggers.
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        await workerManager.UnbindTriggersAsync(notification.IndexedWorkflowTriggers.RemovedTriggers, cancellationToken);
        await workerManager.BindTriggersAsync(notification.IndexedWorkflowTriggers.AddedTriggers, cancellationToken);
    }

    /// Adds, updates and removes workers based on added and removed bookmarks.
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var workflowExecutionContext = notification.IndexedWorkflowBookmarks.WorkflowExecutionContext;
        var removedBookmarks = workflowExecutionContext.MapBookmarks(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        var addedBookmarks = workflowExecutionContext.MapBookmarks(notification.IndexedWorkflowBookmarks.AddedBookmarks);
        await workerManager.UnbindBookmarksAsync(removedBookmarks, cancellationToken);
        await workerManager.BindBookmarksAsync(addedBookmarks, cancellationToken);
    }

    public async Task HandleAsync(BookmarksDeleted notification, CancellationToken cancellationToken)
    {
        await workerManager.UnbindBookmarksAsync(notification.Bookmarks, cancellationToken);
    }
}