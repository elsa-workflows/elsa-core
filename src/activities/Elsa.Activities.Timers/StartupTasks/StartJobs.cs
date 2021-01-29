using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Bookmarks;
using Elsa.Activities.Timers.Services;
using Elsa.Bookmarks;
using Elsa.Services;
using Elsa.Triggers;

namespace Elsa.Activities.Timers.StartupTasks
{
    /// <summary>
    /// Starts Quartz jobs based on workflow blueprints starting with a TimerEvent, CronEvent or StartAtEvent.
    /// </summary>
    public class StartJobs : IStartupTask
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;

        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IWorkflowScheduler _workflowScheduler;

        public StartJobs(IBookmarkFinder bookmarkFinder, ITriggerFinder triggerFinder, IWorkflowScheduler workflowScheduler)
        {
            _bookmarkFinder = bookmarkFinder;
            _workflowScheduler = workflowScheduler;
            _triggerFinder = triggerFinder;
        }

        public int Order => 2000;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await ScheduleTimerEventWorkflowsAsync(cancellationToken);
            await ScheduleCronEventWorkflowsAsync(cancellationToken);
            await ScheduleStartAtWorkflowsAsync(cancellationToken);
        }

        private async Task ScheduleStartAtWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints that start with a run-at.
            var triggerResults = await _triggerFinder.FindTriggersAsync<StartAt>(TenantId, cancellationToken);

            foreach (var result in triggerResults)
            {
                var bookmark = (StartAtBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(result.WorkflowBlueprint.Id, null, result.ActivityId, TenantId, bookmark.ExecuteAt, null, cancellationToken);
            }

            // Schedule workflow instances that are blocked on a start-at.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<StartAt>(TenantId, cancellationToken);

            foreach (var result in bookmarkResults)
            {
                var bookmark = (StartAtBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(null, result.WorkflowInstanceId!, result.ActivityId, TenantId, bookmark.ExecuteAt, null, cancellationToken);
            }
        }

        private async Task ScheduleTimerEventWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints that start with a timer.
            var triggerResults = await _triggerFinder.FindTriggersAsync<Timer>(TenantId, cancellationToken);

            foreach (var result in triggerResults)
            {
                var bookmark = (TimerBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(result.WorkflowBlueprint.Id, null, result.ActivityId, TenantId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken);
            }

            // Schedule workflow instances that are blocked on a timer.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<Timer>(TenantId, cancellationToken);

            foreach (var result in bookmarkResults)
            {
                var bookmark = (TimerBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(null, result.WorkflowInstanceId!, result.ActivityId, TenantId, bookmark.ExecuteAt, null, cancellationToken);
            }
        }

        private async Task ScheduleCronEventWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow blueprints starting with a cron.
            var triggerResults = await _triggerFinder.FindTriggersAsync<Cron>(TenantId, cancellationToken);

            foreach (var result in triggerResults)
            {
                var bookmark = (CronBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(result.WorkflowBlueprint.Id, null, result.ActivityId, TenantId, bookmark.CronExpression, cancellationToken);
            }

            // Schedule workflow instances blocked on a cron event.
            var cronEventTriggers = await _bookmarkFinder.FindBookmarksAsync<Cron>(TenantId, cancellationToken);

            foreach (var result in cronEventTriggers)
            {
                var trigger = (CronBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(null, result.WorkflowInstanceId!, result.ActivityId, TenantId, trigger.ExecuteAt!.Value, null, cancellationToken);
            }
        }
    }
}