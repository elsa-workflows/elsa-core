using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Handlers;

/// <summary>
/// A handler that updates the route table when workflow triggers and bookmarks are indexed.
/// </summary>
public class UpdateRouteTable(IRouteTable routeTable, IOptions<HttpActivityOptions> options) :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        routeTable.RemoveRoutes(notification.IndexedWorkflowTriggers.RemovedTriggers);
        routeTable.AddRoutes(notification.IndexedWorkflowTriggers.AddedTriggers, options);
        routeTable.AddRoutes(notification.IndexedWorkflowTriggers.UnchangedTriggers, options);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        routeTable.RemoveRoutes(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        routeTable.AddRoutes(notification.IndexedWorkflowBookmarks.AddedBookmarks, notification.IndexedWorkflowBookmarks.TenantId);
        return Task.CompletedTask;
    }
}