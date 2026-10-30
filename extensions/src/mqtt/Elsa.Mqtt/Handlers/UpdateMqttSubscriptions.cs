using Elsa.Mediator.Contracts;
using Elsa.Mqtt.Services;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Mqtt.Handlers;

/// <summary>
/// Reacts to workflow trigger/bookmark lifecycle events and keeps MQTT topic subscriptions in sync.
/// </summary>
[UsedImplicitly]
internal class UpdateMqttSubscriptions(IMqttSubscriberManager subscriberManager) :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>,
    INotificationHandler<BookmarksDeleted>
{
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var indexed = notification.IndexedWorkflowTriggers;

        await subscriberManager.UnbindTriggersAsync(indexed.RemovedTriggers, cancellationToken);
        await subscriberManager.BindTriggersAsync(indexed.AddedTriggers, cancellationToken);
    }

    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var ctx = notification.IndexedWorkflowBookmarks.WorkflowExecutionContext;
        var removed = ctx.MapBookmarks(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        var added = ctx.MapBookmarks(notification.IndexedWorkflowBookmarks.AddedBookmarks);

        await subscriberManager.UnbindBookmarksAsync(removed, cancellationToken);
        await subscriberManager.BindBookmarksAsync(added, cancellationToken);
    }

    public async Task HandleAsync(BookmarksDeleted notification, CancellationToken cancellationToken)
    {
        await subscriberManager.UnbindBookmarksAsync(notification.Bookmarks, cancellationToken);
    }
}
