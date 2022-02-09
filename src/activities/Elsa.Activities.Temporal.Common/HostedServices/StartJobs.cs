using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.HostedServices;
using Elsa.Models;
using Elsa.MultiTenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
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

        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger<StartJobs> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly ITenantStore _tenantStore;
        private readonly IServiceScopeFactory _scopeFactory;

        public StartJobs(
            IDistributedLockProvider distributedLockProvider,
            ILogger<StartJobs> logger,
            IBookmarkSerializer bookmarkSerializer,
            ITenantStore tenantStore,
            IServiceScopeFactory scopeFactory)
        {
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
            _bookmarkSerializer = bookmarkSerializer;
            _tenantStore = tenantStore;
            _scopeFactory = scopeFactory;

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

            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);
                await _retryPolicy.ExecuteAsync(async () => await ExecuteInternalAsync(scope.ServiceProvider, tenant, stoppingToken));
            }
        }

        private async Task ExecuteInternalAsync(IServiceProvider serviceProvider, Tenant tenant, CancellationToken cancellationToken)
        {
            var workflowInstanceScheduler = serviceProvider.GetRequiredService<IWorkflowInstanceScheduler>();
            var workflowDefinitionScheduler = serviceProvider.GetRequiredService<IWorkflowDefinitionScheduler>();
            var triggerFinder = serviceProvider.GetRequiredService<ITriggerFinder>();
            var bookmarkFinder = serviceProvider.GetRequiredService<IBookmarkFinder>();

            await ScheduleTimerEventsAsync(workflowInstanceScheduler, workflowDefinitionScheduler, triggerFinder, bookmarkFinder, cancellationToken);
            await ScheduleCronEventsAsync(workflowInstanceScheduler, workflowDefinitionScheduler, triggerFinder, bookmarkFinder, cancellationToken);
            await ScheduleStartAtEventsAsync(workflowInstanceScheduler, workflowDefinitionScheduler, triggerFinder, bookmarkFinder, cancellationToken);
        }

        private async Task ScheduleStartAtEventsAsync(
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<StartAtBookmark>((bookmark, model) =>
                workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId, bookmark.ActivityId, model.ExecuteAt, null, cancellationToken), bookmarkFinder, cancellationToken);
            
            await ScheduleTriggersAsync<StartAtBookmark>((trigger, model) => 
                workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, model.ExecuteAt, null, cancellationToken), triggerFinder, cancellationToken);
        }
        
        private async Task ScheduleTimerEventsAsync(
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<TimerBookmark>((bookmark, model) =>
                workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId, bookmark.ActivityId, model.ExecuteAt, null, cancellationToken), bookmarkFinder, cancellationToken);
            
            await ScheduleTriggersAsync<TimerBookmark>((trigger, model) => 
                workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, model.ExecuteAt, model.Interval, cancellationToken), triggerFinder, cancellationToken);
        }
        
        private async Task ScheduleCronEventsAsync(
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<CronBookmark>((bookmark, model) =>
                workflowInstanceScheduler.ScheduleAsync(bookmark.WorkflowInstanceId!, bookmark.ActivityId, model.CronExpression, cancellationToken), bookmarkFinder, cancellationToken);
            
            await ScheduleTriggersAsync<CronBookmark>((trigger, model) => 
                workflowDefinitionScheduler.ScheduleAsync(trigger.WorkflowDefinitionId, trigger.ActivityId, model.CronExpression, cancellationToken), triggerFinder, cancellationToken);
        }
        
        private async Task ScheduleTriggersAsync<T>(Func<Trigger, T, Task> scheduleAction, ITriggerFinder triggerFinder, CancellationToken cancellationToken) where T : IBookmark
        {
            var results = await triggerFinder.FindTriggersByTypeAsync<T>(TenantId, cancellationToken).ToList();

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

        private async Task ScheduleBookmarksAsync<T>(Func<Bookmark, T, Task> scheduleAction, IBookmarkFinder bookmarkFinder, CancellationToken cancellationToken) where T : IBookmark
        {
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<T>(TenantId, cancellationToken).ToList();

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