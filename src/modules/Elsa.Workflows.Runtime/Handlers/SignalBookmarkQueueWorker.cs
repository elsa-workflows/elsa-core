using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Signals the bookmark queue worker to process any queued work.
/// </summary>
[UsedImplicitly]
public class SignalBookmarkQueueWorker(IBookmarkQueueWorkerSignaler signaler) : INotificationHandler<WorkflowBookmarksIndexed>, INotificationHandler<BookmarkSaved>
{
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        return Trigger();
    }

    public Task HandleAsync(BookmarkSaved notification, CancellationToken cancellationToken)
    {
        return Trigger();
    }

    private Task Trigger()
    {
        signaler.Trigger();
        return Task.CompletedTask;
    }
}