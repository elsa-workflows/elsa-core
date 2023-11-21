using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Http.Handlers;

/// <summary>
/// A handler that updates the route table when workflow triggers and bookmarks are indexed.
/// </summary>
public class UpdateRouteTable :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>
{
    private readonly IRouteTable _routeTable;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRouteTable"/> class.
    /// </summary>
    public UpdateRouteTable(IRouteTable routeTable) => _routeTable = routeTable;

    /// <inheritdoc />
    public Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        _routeTable.RemoveRoutes(notification.IndexedWorkflowTriggers.RemovedTriggers);
        _routeTable.AddRoutes(notification.IndexedWorkflowTriggers.AddedTriggers);
        _routeTable.AddRoutes(notification.IndexedWorkflowTriggers.UnchangedTriggers);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        _routeTable.RemoveRoutes(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        _routeTable.AddRoutes(notification.IndexedWorkflowBookmarks.AddedBookmarks);
        return Task.CompletedTask;
    }
}