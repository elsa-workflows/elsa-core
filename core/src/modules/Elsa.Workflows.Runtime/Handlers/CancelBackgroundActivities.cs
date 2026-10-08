using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// A handler that cancels background activities based on removed bookmarks.
/// </summary>
[UsedImplicitly]
public class CancelBackgroundActivities(IBackgroundActivityScheduler backgroundActivityScheduler) : INotificationHandler<WorkflowBookmarksIndexed>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var removedBookmarks = notification.IndexedWorkflowBookmarks.RemovedBookmarks.Where(x => x.Name == BackgroundActivityInvokerMiddleware.BackgroundActivityBookmarkName);

        foreach (var removedBookmark in removedBookmarks)
        {
            var payload = removedBookmark.GetPayload<BackgroundActivityStimulus>();
            if (payload.JobId != null)
                await backgroundActivityScheduler.UnscheduledAsync(payload.JobId, cancellationToken);
        }
    }
}