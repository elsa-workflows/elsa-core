using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Runtime.Notifications;
using Elsa.Scheduling.Contracts;
using IWorkflowTriggerScheduler = Elsa.Activities.Scheduling.Contracts.IWorkflowTriggerScheduler;

namespace Elsa.Activities.Scheduling.Handlers;

// Updates scheduled jobs based on the updated workflow triggers.
public class ScheduleWorkflowsHandler : INotificationHandler<TriggerIndexingFinished>, INotificationHandler<WorkflowExecuted>
{
    private readonly IWorkflowTriggerScheduler _workflowTriggerScheduler;
    public ScheduleWorkflowsHandler(IWorkflowTriggerScheduler workflowTriggerScheduler) => _workflowTriggerScheduler = workflowTriggerScheduler;
    public async Task HandleAsync(TriggerIndexingFinished notification, CancellationToken cancellationToken) => await _workflowTriggerScheduler.ScheduleTriggersAsync(notification.Triggers, cancellationToken);
    
    public Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}