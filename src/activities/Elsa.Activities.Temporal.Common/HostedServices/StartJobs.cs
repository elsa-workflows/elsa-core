using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.HostedServices;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Polly;
using Polly.Retry;

namespace Elsa.Activities.Temporal.Common.HostedServices
{
    /// <summary>
    /// Starts jobs based on triggers & bookmarks for Timer, Cron or StartAt activity.
    /// </summary>
    public class StartJobs : IScopedBackgroundService
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string? TenantId = default;

        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IWorkflowInstanceScheduler _workflowInstanceScheduler;
        private readonly IWorkflowDefinitionScheduler _workflowDefinitionScheduler;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger<StartJobs> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public StartJobs(
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IBookmarkSerializer bookmarkSerializer,
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            IDistributedLockProvider distributedLockProvider,
            ILogger<StartJobs> logger)
        {
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _bookmarkSerializer = bookmarkSerializer;
            _workflowInstanceScheduler = workflowInstanceScheduler;
            _workflowDefinitionScheduler = workflowDefinitionScheduler;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5)
                );
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(StartJobs), null, stoppingToken);

            if (handle == null)
                return;

            await _retryPolicy.ExecuteAsync(async () => await ExecuteInternalAsync(stoppingToken));
        }

        private async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            await ScheduleTimerEventsAsync(cancellationToken);
            await ScheduleCronEventsAsync(cancellationToken);
            await ScheduleStartAtEventsAsync(cancellationToken);
        }

        private async Task ScheduleStartAtEventsAsync(CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<StartAtBookmark>((bookmark, model) =>
                _workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId, bookmark.ActivityId, model.ExecuteAt, null, cancellationToken), cancellationToken);
            
            await ScheduleTriggersAsync<StartAtBookmark>((trigger, model) => 
                _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, model.ExecuteAt, null, cancellationToken), cancellationToken);
        }
        
        private async Task ScheduleTimerEventsAsync(CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<TimerBookmark>((bookmark, model) =>
                _workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId, bookmark.ActivityId, model.ExecuteAt, null, cancellationToken), cancellationToken);
            
            await ScheduleTriggersAsync<TimerBookmark>((trigger, model) => 
                _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, model.ExecuteAt, model.Interval, cancellationToken), cancellationToken);
        }
        
        private async Task ScheduleCronEventsAsync(CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<CronBookmark>((bookmark, model) =>
                _workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId!, bookmark.ActivityId, model.CronExpression, cancellationToken), cancellationToken);
            
            await ScheduleTriggersAsync<CronBookmark>((trigger, model) => 
                _workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, model.CronExpression, cancellationToken), cancellationToken);
        }
        
        private async Task ScheduleTriggersAsync<T>(Func<Trigger, T, Task> scheduleAction, CancellationToken cancellationToken) where T : IBookmark
        {
            var results = await _triggerFinder.FindTriggersByTypeAsync<T>(TenantId, cancellationToken).ToList();

            _logger.LogDebug("Found {TriggerResultCount} triggers for Timer", results.Count);
            var index = 0;

            foreach (var result in results)
            {
                var bookmark = _bookmarkSerializer.Deserialize<T>(result.Model);
                await scheduleAction(result, bookmark);

                index++;
                _logger.LogDebug("Scheduled {CurrentTriggerIndex} of {TriggerResultCount}", index, results.Count);
            }
        }

        private async Task ScheduleBookmarksAsync<T>(Func<Bookmark, T, Task> scheduleAction, CancellationToken cancellationToken) where T : IBookmark
        {
            var bookmarks = await _bookmarkFinder.FindBookmarksByTypeAsync<T>(TenantId, cancellationToken).ToList();

            _logger.LogDebug("Found {BookmarkResultCount} bookmarks for {BookmarkType}", bookmarks.Count, typeof(T).Name);
            var index = 0;

            foreach (var result in bookmarks)
            {
                var bookmark = _bookmarkSerializer.Deserialize<T>(result.Model);
                await scheduleAction(result, bookmark);

                index++;
                _logger.LogDebug("Scheduled {CurrentBookmarkIndex} of {BookmarkResultCount}", index, bookmarks.Count);
            }
        }
    }
}