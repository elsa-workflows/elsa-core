using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Http.Handlers;

public class UpdateRouteTable :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>
{
    private readonly IRouteTable _routeTable;
    public UpdateRouteTable(IRouteTable routeTable) => _routeTable = routeTable;

    public Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        _routeTable.RemoveRoutes(notification.IndexedWorkflowTriggers.RemovedTriggers);
        _routeTable.AddRoutes(notification.IndexedWorkflowTriggers.AddedTriggers);
        _routeTable.AddRoutes(notification.IndexedWorkflowTriggers.UnchangedTriggers);
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        _routeTable.RemoveRoutes(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        _routeTable.AddRoutes(notification.IndexedWorkflowBookmarks.AddedBookmarks);
        return Task.CompletedTask;
    }
}