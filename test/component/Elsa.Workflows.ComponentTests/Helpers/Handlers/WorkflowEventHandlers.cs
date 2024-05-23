using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Notifications;
using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Handlers;

[UsedImplicitly]
public class WorkflowEventHandlers(IWorkflowEvents workflowEvents) : 
    INotificationHandler<WorkflowFinished>,
    INotificationHandler<WorkflowInstanceSaved>
{
    public Task HandleAsync(WorkflowFinished notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnWorkflowFinished(new WorkflowFinishedEventArgs(notification.Workflow, notification.WorkflowState));
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        workflowEvents.OnWorkflowInstanceSaved(new WorkflowInstanceSavedEventArgs(notification.WorkflowInstance));
        return Task.CompletedTask;
    }
}