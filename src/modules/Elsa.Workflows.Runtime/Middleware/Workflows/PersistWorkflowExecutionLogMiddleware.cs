using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of persisting workflow execution log entries.
/// </summary>
public class PersistWorkflowExecutionLogMiddleware : WorkflowExecutionMiddleware
{
    private readonly IWorkflowExecutionLogStore _workflowExecutionLogStore;
    private readonly INotificationSender _notificationSender;
    private readonly IIdentityGenerator _identityGenerator;

    /// <inheritdoc />
    public PersistWorkflowExecutionLogMiddleware(
        WorkflowMiddlewareDelegate next,
        IWorkflowExecutionLogStore workflowExecutionLogStore,
        INotificationSender notificationSender,
        IIdentityGenerator identityGenerator) : base(next)
    {
        _workflowExecutionLogStore = workflowExecutionLogStore;
        _notificationSender = notificationSender;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Persist workflow execution log entries.
        var entries = context.ExecutionLog.Select(x => new WorkflowExecutionLogRecord
        {
            Id = _identityGenerator.GenerateId(),
            ActivityInstanceId = x.ActivityInstanceId,
            ParentActivityInstanceId = x.ParentActivityInstanceId,
            NodeId = x.NodeId,
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

        await _workflowExecutionLogStore.SaveManyAsync(entries, context.CancellationToken);
        
        // Publish notification.
        await _notificationSender.SendAsync(new WorkflowExecutionLogUpdated(context));
    }
}