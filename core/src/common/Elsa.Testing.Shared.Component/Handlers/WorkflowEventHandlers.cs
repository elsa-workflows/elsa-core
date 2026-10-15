using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Testing.Shared.Handlers;

[UsedImplicitly]
public class WorkflowEventHandlers(WorkflowEvents workflowEvents) : 
    INotificationHandler<WorkflowFinished>,
    INotificationHandler<WorkflowInstanceSaved>,
    INotificationHandler<WorkflowStateCommitted>,
    INotificationHandler<ActivityExecuted>,
    INotificationHandler<ActivityExecutionLogUpdated>
{
    public Task HandleAsync(WorkflowFinished notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnWorkflowFinished(new(notification.Workflow, notification.WorkflowState));
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnWorkflowInstanceSaved(new(notification.WorkflowInstance));
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnWorkflowStateCommitted(new(notification.WorkflowExecutionContext, notification.WorkflowState, notification.WorkflowInstance));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ActivityExecuted notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnActivityExecuted(new(notification.ActivityExecutionContext));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ActivityExecutionLogUpdated notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnActivityExecutedLogUpdated(new(notification.WorkflowExecutionContext, notification.Records));
        return Task.CompletedTask;
    }
}