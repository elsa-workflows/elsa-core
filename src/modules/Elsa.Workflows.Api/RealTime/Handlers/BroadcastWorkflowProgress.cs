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
public class BroadcastWorkflowProgress :
    INotificationHandler<ActivityExecutionLogUpdated>,
    INotificationHandler<ActivityExecutionRecordDeleted>,
    INotificationHandler<ActivityExecutionRecordUpdated>,
    INotificationHandler<WorkflowExecutionLogUpdated>,
    INotificationHandler<WorkflowInstanceSaved>
{
    private readonly IHubContext<WorkflowInstanceHub, IWorkflowInstanceClient> _hubContext;
    private readonly IActivityExecutionStatsService _activityExecutionStatsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BroadcastWorkflowProgress"/> class.
    /// </summary>
    public BroadcastWorkflowProgress(IHubContext<WorkflowInstanceHub, IWorkflowInstanceClient> hubContext, IActivityExecutionStatsService activityExecutionStatsService)
    {
        _hubContext = hubContext;
        _activityExecutionStatsService = activityExecutionStatsService;
    }

    /// <inheritdoc />
    public async Task HandleAsync(ActivityExecutionLogUpdated notification, CancellationToken cancellationToken)
    {
        var workflowInstanceId = notification.WorkflowExecutionContext.Id;
        var activityIds = notification.Records.Select(x => x.ActivityId).Distinct().ToList();
        var stats = (await _activityExecutionStatsService.GetStatsAsync(workflowInstanceId, activityIds, cancellationToken)).ToList();
        var clients = _hubContext.Clients.Group(workflowInstanceId);
        var message = new ActivityExecutionLogUpdatedMessage(stats);

        await clients.ActivityExecutionLogUpdatedAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(ActivityExecutionRecordDeleted notification, CancellationToken cancellationToken)
    {
        var record = notification.Record;
        var workflowInstanceId = record.WorkflowInstanceId;
        var activityId = record.ActivityId;
        await BroadcastActivityExecutionLogUpdatedAsync(workflowInstanceId, activityId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(ActivityExecutionRecordUpdated notification, CancellationToken cancellationToken)
    {
        var record = notification.Record;
        var workflowInstanceId = record.WorkflowInstanceId;
        var activityId = record.ActivityId;
        await BroadcastActivityExecutionLogUpdatedAsync(workflowInstanceId, activityId, cancellationToken);
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
    
    private async Task BroadcastActivityExecutionLogUpdatedAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken)
    {
        var stats = await _activityExecutionStatsService.GetStatsAsync(workflowInstanceId, activityId, cancellationToken);
        var clients = _hubContext.Clients.Group(workflowInstanceId);
        var message = new ActivityExecutionLogUpdatedMessage(new[] { stats });

        await clients.ActivityExecutionLogUpdatedAsync(message, cancellationToken);
    }
}