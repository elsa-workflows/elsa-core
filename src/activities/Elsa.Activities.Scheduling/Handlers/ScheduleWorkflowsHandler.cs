using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Runtime.Notifications;

namespace Elsa.Activities.Scheduling.Handlers;

// Updates scheduled jobs based on the updated workflow triggers.
public class ScheduleWorkflowsHandler : INotificationHandler<TriggerIndexingFinished>, INotificationHandler<WorkflowExecuted>
{
    private readonly IWorkflowTriggerScheduler _workflowTriggerScheduler;
    private readonly IWorkflowBookmarkScheduler _workflowBookmarkScheduler;

    public ScheduleWorkflowsHandler(IWorkflowTriggerScheduler workflowTriggerScheduler, IWorkflowBookmarkScheduler workflowBookmarkScheduler)
    {
        _workflowTriggerScheduler = workflowTriggerScheduler;
        _workflowBookmarkScheduler = workflowBookmarkScheduler;
    }

    public async Task HandleAsync(TriggerIndexingFinished notification, CancellationToken cancellationToken) => await _workflowTriggerScheduler.ScheduleTriggersAsync(notification.Triggers, cancellationToken);

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowExecutionContext = notification.WorkflowExecutionContext;
        var workflowInstanceId = workflowExecutionContext.Id;
        var bookmarks = workflowExecutionContext.Bookmarks;
        await _workflowBookmarkScheduler.ScheduleBookmarksAsync(workflowInstanceId, bookmarks, cancellationToken);
    }
}