using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Signals the bookmark queue worker to process any queued work.
/// </summary>
[UsedImplicitly]
public class SignalBookmarkQueueWorker(IBookmarkQueueSignaler signaler) :
    INotificationHandler<WorkflowBookmarksIndexed>,
    INotificationHandler<BookmarkSaved>,
    INotificationHandler<WorkflowInstanceSaved>,
    INotificationHandler<WorkflowBookmarksPersisted>
{
    /// <inheritdoc />
    public Task HandleAsync(BookmarkSaved notification, CancellationToken cancellationToken)
    {
        return signaler.TriggerAsync(cancellationToken);
    }

    /// <summary>
    /// Retained for compatibility. Queue signaling uses <see cref="WorkflowBookmarksPersisted"/>.
    /// </summary>
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retained for compatibility. A workflow instance save does not imply a bookmark was added.
    /// </summary>
    public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Signals the queue when new bookmarks are available after the commit.
    /// </summary>
    public Task HandleAsync(WorkflowBookmarksPersisted notification, CancellationToken cancellationToken)
    {
        return notification.Diff.Added.Count > 0 ? signaler.TriggerAsync(cancellationToken) : Task.CompletedTask;
    }
}