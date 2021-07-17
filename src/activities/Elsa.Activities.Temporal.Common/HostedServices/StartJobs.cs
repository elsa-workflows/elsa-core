using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.HostedServices;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Temporal.Common.HostedServices
{
    /// <summary>
    /// Starts jobs based on workflow instances blocked on a Timer, Cron or StartAt activity.
    /// </summary>
    public class StartJobs : IScopedBackgroundService
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string? TenantId = default;

        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly IWorkflowInstanceScheduler _workflowScheduler;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger<StartJobs> _logger;

        public StartJobs(IBookmarkFinder bookmarkFinder, IWorkflowInstanceScheduler workflowScheduler, IDistributedLockProvider distributedLockProvider, ILogger<StartJobs> logger)
        {
            _bookmarkFinder = bookmarkFinder;
            _workflowScheduler = workflowScheduler;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(StartJobs), null, stoppingToken);

            if (handle == null)
                return;

            await ScheduleTimerEventWorkflowsAsync(stoppingToken);
            await ScheduleCronEventWorkflowsAsync(stoppingToken);
            await ScheduleStartAtWorkflowsAsync(stoppingToken);
        }

        private async Task ScheduleStartAtWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow instances that are blocked on a start-at.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<StartAt>(tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for StartAt", bookmarkResults.Count);
            var index = 0;

            foreach (var result in bookmarkResults)
            {
                var bookmark = (StartAtBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, bookmark.ExecuteAt, null, cancellationToken);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarkResults.Count);
            }
        }

        private async Task ScheduleTimerEventWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow instances that are blocked on a timer.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<Timer>(tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for Timer", bookmarkResults.Count);
            var index = 0;

            foreach (var result in bookmarkResults)
            {
                var bookmark = (TimerBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, bookmark.ExecuteAt, null, cancellationToken);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarkResults.Count);
            }
        }

        private async Task ScheduleCronEventWorkflowsAsync(CancellationToken cancellationToken)
        {
            // Schedule workflow instances blocked on a cron event.
            var bookmarkResults = await _bookmarkFinder.FindBookmarksAsync<Cron>(tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for StartAt", bookmarkResults.Count);
            var index = 0;

            foreach (var result in bookmarkResults)
            {
                var trigger = (CronBookmark) result.Bookmark;
                await _workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, trigger.ExecuteAt!.Value, null, cancellationToken);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarkResults.Count);
            }
        }
    }
}