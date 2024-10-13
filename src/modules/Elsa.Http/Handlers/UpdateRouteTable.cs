using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Handlers;

/// <summary>
/// A handler that updates the route table when workflow triggers and bookmarks are indexed.
/// </summary>
[UsedImplicitly]
public class UpdateRouteTable(IRouteTableUpdater routeTableUpdater, IOptions<HttpActivityOptions> options) :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        routeTableUpdater.RemoveRoutes(notification.IndexedWorkflowTriggers.RemovedTriggers);
        await routeTableUpdater.AddRoutesAsync(notification.IndexedWorkflowTriggers.AddedTriggers, cancellationToken);
        await routeTableUpdater.AddRoutesAsync(notification.IndexedWorkflowTriggers.UnchangedTriggers, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        routeTableUpdater.RemoveRoutes(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        await routeTableUpdater.AddRoutesAsync(notification.IndexedWorkflowBookmarks.AddedBookmarks, notification.IndexedWorkflowBookmarks.WorkflowExecutionContext, cancellationToken);
    }
}