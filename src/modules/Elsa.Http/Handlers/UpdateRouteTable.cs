using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
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
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRouteTable"/> class.
    /// </summary>
    public UpdateRouteTable(
        IRouteTable routeTable, 
        IWorkflowDefinitionStore workflowDefinitionStore,
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore)
    {
        _routeTable = routeTable;
        _workflowDefinitionStore = workflowDefinitionStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var triggers = notification.IndexedWorkflowTriggers;

        await EnsureRouteDataIntegrityAsync(triggers.Workflow.Id, triggers.RemovedTriggers, cancellationToken);
        
        _routeTable.RemoveRoutes(triggers.RemovedTriggers);
        _routeTable.AddRoutes(triggers.AddedTriggers);
        _routeTable.AddRoutes(triggers.UnchangedTriggers);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        _routeTable.RemoveRoutes(notification.IndexedWorkflowBookmarks.RemovedBookmarks);
        _routeTable.AddRoutes(notification.IndexedWorkflowBookmarks.AddedBookmarks);
        
        return Task.CompletedTask;
    }
    
    private async Task EnsureRouteDataIntegrityAsync(string workflowId, ICollection<StoredTrigger> removedTriggers, CancellationToken cancellationToken)
    {
        // Filter out triggers with routes that are still used by other workflows.
        
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var paths = removedTriggers
            .Where(x => x.Name == triggerName && x.Payload != null)
            .Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        
        var workflowDefinitions = (await _workflowDefinitionStore.FindManyAsync(new WorkflowDefinitionFilter { VersionOptions = VersionOptions.Published }, cancellationToken)).ToList();
        var triggers = await _triggerStore.FindManyAsync(new TriggerFilter { WorkflowDefinitionIds = workflowDefinitions.Select(x => x.DefinitionId).ToList() }, cancellationToken);

        foreach (var path in paths)
        {
            if (workflowDefinitions.Any(x => x.Id != workflowId && triggers
                    .Where(t => t.WorkflowDefinitionId == x.DefinitionId)
                    .Any(t => t.Payload != null && t.GetPayload<HttpEndpointBookmarkPayload>().Path == path)))
            {
                removedTriggers.RemoveWhere(x => x.Name == triggerName && x.Payload != null && x.GetPayload<HttpEndpointBookmarkPayload>().Path == path);
            }
        }
    }
}