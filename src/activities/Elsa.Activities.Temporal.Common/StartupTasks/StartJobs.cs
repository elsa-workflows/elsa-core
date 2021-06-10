using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Temporal.Common.StartupTasks
{
    /// <summary>
    /// Starts jobs based on workflow instances blocked on a Timer, Cron or StartAt activity.
    /// </summary>
    public class StartJobs : IStartupTask
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string? TenantId = default;

        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly IWorkflowInstanceScheduler _workflowScheduler;

        public StartJobs(IBookmarkFinder bookmarkFinder, IWorkflowInstanceScheduler workflowScheduler)
        {
            _bookmarkFinder = bookmarkFinder;
            _workflowScheduler = workflowScheduler;
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
            // Schedule workflow instances that are blocked on a start-at.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<StartAt>(tenantId: TenantId, cancellationToken: cancellationToken);

            foreach (var result in bookmarkResults)
            {
                var bookmark = (StartAtBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, bookmark.ExecuteAt, null, cancellationToken);
            }
        }

        private async Task ScheduleTimerEventWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow instances that are blocked on a timer.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<Timer>(tenantId: TenantId, cancellationToken: cancellationToken);

            foreach (var result in bookmarkResults)
            {
                var bookmark = (TimerBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, bookmark.ExecuteAt, null, cancellationToken);
            }
        }

        private async Task ScheduleCronEventWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow instances blocked on a cron event.
            var cronEventTriggers = await _bookmarkFinder.FindBookmarksAsync<Cron>(tenantId: TenantId, cancellationToken: cancellationToken);

            foreach (var result in cronEventTriggers)
            {
                var trigger = (CronBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, trigger.ExecuteAt!.Value, null, cancellationToken);
            }
        }
    }
}