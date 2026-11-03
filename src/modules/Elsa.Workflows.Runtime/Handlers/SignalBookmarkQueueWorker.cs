using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Signals the bookmark queue worker to process any queued work.
/// </summary>
[UsedImplicitly]
public class SignalBookmarkQueueWorker(IBookmarkQueueSignaler signaler) : INotificationHandler<WorkflowBookmarksIndexed>, INotificationHandler<BookmarkSaved>, INotificationHandler<WorkflowInstanceSaved>, INotificationHandler<WorkflowBookmarksCommitted>
{
    /// <summary>
    /// Wakes the queue after an explicitly saved bookmark becomes available.
    /// </summary>
    public Task HandleAsync(BookmarkSaved notification, CancellationToken cancellationToken)
    {
        return signaler.TriggerAsync(cancellationToken);
    }

    /// <summary>
    /// Retained for compatibility. Indexing precedes the workflow state save; delivery is signaled after that save.
    /// </summary>
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retained for compatibility. Generic workflow saves do not imply that new bookmarks are available.
    /// </summary>
    public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Wakes the queue for new bookmarks only after their owning workflow state has been persisted.
    /// </summary>
    public Task HandleAsync(WorkflowBookmarksCommitted notification, CancellationToken cancellationToken)
    {
        return notification.Diff.Added.Count > 0 ? signaler.TriggerAsync(cancellationToken) : Task.CompletedTask;
    }
}