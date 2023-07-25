using Elsa.Mediator.Contracts;
using Elsa.Workflows.Api.RealTime.Contracts;
using Elsa.Workflows.Api.RealTime.Hubs;
using Elsa.Workflows.Api.RealTime.Messages;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Workflows.Api.RealTime.Handlers;

/// <summary>
/// Broadcasts workflow progress to clients.
/// </summary>
public class BroadcastWorkflowProgress : INotificationHandler<ActivityExecutionLogUpdated>, INotificationHandler<WorkflowExecutionLogUpdated>, INotificationHandler<WorkflowInstanceSaved>
{
    private readonly IHubContext<WorkflowInstanceHub, IWorkflowInstanceClient> _hubContext;
    private readonly IActivityExecutionService _activityExecutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BroadcastWorkflowProgress"/> class.
    /// </summary>
    public BroadcastWorkflowProgress(IHubContext<WorkflowInstanceHub, IWorkflowInstanceClient> hubContext, IActivityExecutionService activityExecutionService)
    {
        _hubContext = hubContext;
        _activityExecutionService = activityExecutionService;
    }

    /// <inheritdoc />
    public async Task HandleAsync(ActivityExecutionLogUpdated notification, CancellationToken cancellationToken)
    {
        var workflowInstanceId = notification.WorkflowExecutionContext.Id;
        var activityIds = notification.Records.Select(x => x.ActivityId).Distinct().ToList();
        var stats = (await _activityExecutionService.GetStatsAsync(workflowInstanceId, activityIds, cancellationToken)).ToList();
        var clients = _hubContext.Clients.Group(workflowInstanceId);
        var message = new ActivityExecutionLogUpdatedMessage(stats);
        
        await clients.ActivityExecutionLogUpdatedAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowExecutionLogUpdated notification, CancellationToken cancellationToken)
    {
        var workflowInstanceId = notification.WorkflowExecutionContext.Id;
        var clients = _hubContext.Clients.Group(workflowInstanceId);
        var message = new WorkflowExecutionLogUpdatedMessage();
        
        await clients.WorkflowExecutionLogUpdatedAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        var workflowInstanceId = notification.WorkflowInstance.Id;
        var clients = _hubContext.Clients.Group(workflowInstanceId);
        var message = new WorkflowInstanceUpdatedMessage(workflowInstanceId);
        
        await clients.WorkflowInstanceUpdatedAsync(message, cancellationToken);
    }
}