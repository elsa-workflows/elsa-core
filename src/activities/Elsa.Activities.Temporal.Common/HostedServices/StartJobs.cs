using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Messages;
using Elsa.HostedServices;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Rebus.Extensions;

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
        private readonly ICommandSender _commandSender;
        private readonly ILogger<StartJobs> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ITenantStore _tenantStore;
        private readonly IServiceScopeFactory _scopeFactory;

        public StartJobs(
            IDistributedLockProvider distributedLockProvider,
            ICommandSender commandSender,
            ITenantStore tenantStore,
            IServiceScopeFactory scopeFactory)
        {
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
            _logger = logger;
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
            await ScheduleBookmarksAsync<StartAtBookmark>(cancellationToken);
            await ScheduleTriggersAsync<StartAtBookmark>(cancellationToken);
        }

        private async Task ScheduleTimerEventsAsync(
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<TimerBookmark>(cancellationToken);
            await ScheduleTriggersAsync<TimerBookmark>(cancellationToken);
        }

        private async Task ScheduleCronEventsAsync(
            IWorkflowInstanceScheduler workflowInstanceScheduler,
            IWorkflowDefinitionScheduler workflowDefinitionScheduler,
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<CronBookmark>(cancellationToken);
            await ScheduleTriggersAsync<CronBookmark>(cancellationToken);
        }

        private async Task ScheduleTriggersAsync<T>(ITriggerFinder triggerFinder, CancellationToken cancellationToken) where T : IBookmark
        {
            var stream = triggerFinder.StreamTriggersByTypeAsync(typeof(T).GetSimpleAssemblyQualifiedName(), TenantId, cancellationToken);
            var index = 0;

            await foreach (var result in stream.WithCancellation(cancellationToken))
            {
                await _commandSender.SendAsync(new ScheduleTemporalTrigger(result), cancellationToken: cancellationToken);
                _logger.LogDebug("Dispatched scheduling trigger {CurrentTriggerCount}", ++index);
            }
        }

        private async Task ScheduleBookmarksAsync<T>(IBookmarkFinder bookmarkFinder, CancellationToken cancellationToken) where T : IBookmark
        {
            var stream = bookmarkFinder.StreamBookmarksByTypeAsync(typeof(T).GetSimpleAssemblyQualifiedName(), TenantId, cancellationToken);
            var index = 0;

            await foreach (var result in stream.WithCancellation(cancellationToken))
            {
                await _commandSender.SendAsync(new ScheduleTemporalBookmark(result), cancellationToken: cancellationToken);
                _logger.LogDebug("Dispatched scheduling bookmark {CurrentBookmarkIndex}", ++index);
            }
        }
    }
}