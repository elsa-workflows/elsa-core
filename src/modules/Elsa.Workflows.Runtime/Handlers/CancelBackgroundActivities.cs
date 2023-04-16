using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Models.Bookmarks;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// A handler that cancels background activities.
/// </summary>
[PublicAPI]
public class CancelBackgroundActivities : INotificationHandler<WorkflowBookmarksIndexed>
{
    private readonly IBackgroundActivityScheduler _backgroundActivityScheduler;
    private readonly IBookmarkPayloadSerializer _bookmarkPayloadSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledBackgroundActivity"/> class.
    /// </summary>
    public CancelBackgroundActivities(IBackgroundActivityScheduler backgroundActivityScheduler, IBookmarkPayloadSerializer bookmarkPayloadSerializer)
    {
        _backgroundActivityScheduler = backgroundActivityScheduler;
        _bookmarkPayloadSerializer = bookmarkPayloadSerializer;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var removedBookmarks = notification.IndexedWorkflowBookmarks.RemovedBookmarks.Where(x => x.Name == BackgroundActivityInvokerMiddleware.BackgroundActivityBookmarkName);

        foreach (var removedBookmark in removedBookmarks)
        {
            var payload = removedBookmark.GetPayload<BackgroundActivityBookmark>();
            if (payload.JobId != null) 
                await _backgroundActivityScheduler.CancelAsync(payload.JobId, cancellationToken);
        }
    }
}