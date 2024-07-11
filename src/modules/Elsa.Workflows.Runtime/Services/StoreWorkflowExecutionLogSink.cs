using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// This implementation saves <see cref="WorkflowExecutionLogRecord"/> directly through the store.
/// </summary>
public class StoreWorkflowExecutionLogSink(IWorkflowExecutionLogStore store, IIdentityGenerator identityGenerator, INotificationSender notificationSender) : IWorkflowExecutionLogSink
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        var records = context.ExecutionLog.Select(x => new WorkflowExecutionLogRecord
        {
            Id = identityGenerator.GenerateId(),
            ActivityInstanceId = x.ActivityInstanceId,
            ParentActivityInstanceId = x.ParentActivityInstanceId,
            ActivityNodeId = x.NodeId,
            ActivityId = x.ActivityId,
            ActivityType = x.ActivityType,
            ActivityTypeVersion = x.ActivityTypeVersion,
            ActivityName = x.ActivityName,
            Message = x.Message,
            EventName = x.EventName,
            WorkflowDefinitionId = context.Workflow.Identity.DefinitionId,
            WorkflowDefinitionVersionId = context.Workflow.Identity.Id,
            WorkflowInstanceId = context.Id,
            WorkflowVersion = context.Workflow.Version,
            Source = x.Source,
            ActivityState = x.ActivityState,
            Payload = x.Payload,
            Timestamp = x.Timestamp,
            Sequence = x.Sequence
        }).ToList();
        
        await store.AddManyAsync(records, context.CancellationToken);
        await notificationSender.SendAsync(new WorkflowExecutionLogUpdated(context), context.CancellationToken);
    }
}