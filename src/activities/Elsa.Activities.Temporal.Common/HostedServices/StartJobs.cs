using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.HostedServices;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Polly;
using Polly.Retry;

namespace Elsa.Activities.Temporal.Common.HostedServices
{
    /// <summary>
    /// Starts jobs based on workflow instances blocked on a Timer, Cron or StartAt activity.
    /// </summary>
    public class StartJobs : IScopedBackgroundService
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string? TenantId = default;

        protected readonly IServiceScopeFactory _scopeFactory;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger<StartJobs> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public StartJobs(IDistributedLockProvider distributedLockProvider, ILogger<StartJobs> logger, IServiceScopeFactory scopeFactory)
        {
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
            _scopeFactory = scopeFactory;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt =>
                    TimeSpan.FromSeconds(5)
                );
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(StartJobs), null, stoppingToken);

            if (handle == null)
                return;

            await _retryPolicy.ExecuteAsync(async () => await ExecuteInternalAsync(stoppingToken));
        }

        protected virtual async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            await ScheduleWorkflowsAsync(scope.ServiceProvider, cancellationToken);
        }

        protected async Task ScheduleWorkflowsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var bookmarkFinder = serviceProvider.GetRequiredService<IBookmarkFinder>();
            var workflowScheduler = serviceProvider.GetRequiredService<IWorkflowInstanceScheduler>();

            await ScheduleTimerEventWorkflowsAsync(bookmarkFinder, workflowScheduler, cancellationToken);
            await ScheduleCronEventWorkflowsAsync(bookmarkFinder, workflowScheduler, cancellationToken);
            await ScheduleStartAtWorkflowsAsync(bookmarkFinder, workflowScheduler, cancellationToken);
        }

        private async Task ScheduleStartAtWorkflowsAsync(IBookmarkFinder bookmarkFinder, IWorkflowInstanceScheduler workflowScheduler, CancellationToken cancellationToken)
        {
            // Schedule workflow instances that are blocked on a start-at.
            var bookmarkResults = await bookmarkFinder.FindBookmarksAsync<StartAt>(tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for StartAt", bookmarkResults.Count);
            var index = 0;

            foreach (var result in bookmarkResults)
            {
                var bookmark = (StartAtBookmark)result.Bookmark;
                await workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, bookmark.ExecuteAt, null, cancellationToken);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarkResults.Count);
            }
        }

        private async Task ScheduleTimerEventWorkflowsAsync(IBookmarkFinder bookmarkFinder, IWorkflowInstanceScheduler workflowScheduler, CancellationToken cancellationToken)
        {
            // Schedule workflow instances that are blocked on a timer.
            var bookmarkResults = await bookmarkFinder.FindBookmarksAsync<Timer>(tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for Timer", bookmarkResults.Count);
            var index = 0;

            foreach (var result in bookmarkResults)
            {
                var bookmark = (TimerBookmark)result.Bookmark;
                await workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, bookmark.ExecuteAt, null, cancellationToken);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarkResults.Count);
            }
        }

        private async Task ScheduleCronEventWorkflowsAsync(IBookmarkFinder bookmarkFinder, IWorkflowInstanceScheduler workflowScheduler, CancellationToken cancellationToken)
        {
            // Schedule workflow instances blocked on a cron event.
            var bookmarkResults = await bookmarkFinder.FindBookmarksAsync<Cron>(tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for StartAt", bookmarkResults.Count);
            var index = 0;

            foreach (var result in bookmarkResults)
            {
                var trigger = (CronBookmark)result.Bookmark;
                await workflowScheduler.ScheduleAsync(result.WorkflowInstanceId!, result.ActivityId, trigger.ExecuteAt!.Value, null, cancellationToken);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarkResults.Count);
            }
        }
    }
}