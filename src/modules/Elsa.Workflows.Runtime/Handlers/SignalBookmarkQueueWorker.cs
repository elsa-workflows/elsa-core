using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Signals the bookmark queue worker to process any queued work.
/// </summary>
[UsedImplicitly]
public class SignalBookmarkQueueWorker(IBookmarkQueueSignaler signaler) : INotificationHandler<WorkflowBookmarksIndexed>, INotificationHandler<BookmarkSaved>
{
    public Task HandleAsync(BookmarkSaved notification, CancellationToken cancellationToken)
    {
        return Trigger();
    }

    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        return Trigger();
    }

    private async Task Trigger()
    {
        await signaler.TriggerAsync();
    }
}