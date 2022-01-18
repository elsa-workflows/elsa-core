using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Runtime.Notifications;

namespace Elsa.Activities.Scheduling.Handlers;

// Updates scheduled jobs based on the updated workflow triggers.
public class ScheduleWorkflowsHandler : INotificationHandler<TriggerIndexingFinished>
{
    private readonly IWorkflowTriggerScheduler _workflowTriggerScheduler;
    public ScheduleWorkflowsHandler(IWorkflowTriggerScheduler workflowTriggerScheduler) => _workflowTriggerScheduler = workflowTriggerScheduler;
    public async Task HandleAsync(TriggerIndexingFinished notification, CancellationToken cancellationToken) => await _workflowTriggerScheduler.ScheduleTriggersAsync(notification.Triggers, cancellationToken);
}