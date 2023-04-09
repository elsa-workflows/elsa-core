using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Scheduling.Handlers;

/// <summary>
/// Updates scheduled jobs based on updated workflow triggers and bookmarks.
/// </summary>
public class ScheduleWorkflows : INotificationHandler<WorkflowTriggersIndexed>, INotificationHandler<WorkflowBookmarksIndexed>
{
    private readonly ITriggerScheduler _triggerScheduler;
    private readonly IBookmarkScheduler _bookmarkScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleWorkflows"/> class.
    /// </summary>
    public ScheduleWorkflows(ITriggerScheduler triggerScheduler, IBookmarkScheduler bookmarkScheduler)
    {
        _triggerScheduler = triggerScheduler;
        _bookmarkScheduler = bookmarkScheduler;
    }

    /// <summary>
    /// Updates scheduled jobs based on updated triggers.
    /// </summary>
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        await _triggerScheduler.UnscheduleAsync(notification.IndexedWorkflowTriggers.RemovedTriggers, cancellationToken);
        await _triggerScheduler.ScheduleAsync(notification.IndexedWorkflowTriggers.AddedTriggers, cancellationToken);
    }

    /// <summary>
    /// Updates scheduled jobs based on updated bookmarks.
    /// </summary>
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var workflowInstanceId = notification.IndexedWorkflowBookmarks.InstanceId;
        await _bookmarkScheduler.UnscheduleAsync(workflowInstanceId, notification.IndexedWorkflowBookmarks.RemovedBookmarks, cancellationToken);
        await _bookmarkScheduler.ScheduleAsync(workflowInstanceId, notification.IndexedWorkflowBookmarks.AddedBookmarks, cancellationToken);
    }
}