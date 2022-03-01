using System;
using System.Threading;
using System.Threading.Tasks;
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
            IServiceScopeFactory scopeFactory,
            ILogger<StartJobs> logger)
        {
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
            _tenantStore = tenantStore;
            _scopeFactory = scopeFactory;
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

            foreach (var tenant in await _tenantStore.GetTenantsAsync())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);
                await _retryPolicy.ExecuteAsync(async () => await ExecuteInternalAsync(scope.ServiceProvider, stoppingToken));
            }
        }

        private async Task ExecuteInternalAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var triggerFinder = serviceProvider.GetRequiredService<ITriggerFinder>();
            var bookmarkFinder = serviceProvider.GetRequiredService<IBookmarkFinder>();

            await ScheduleTimerEventsAsync(triggerFinder, bookmarkFinder, cancellationToken);
            await ScheduleCronEventsAsync(triggerFinder, bookmarkFinder, cancellationToken);
            await ScheduleStartAtEventsAsync(triggerFinder, bookmarkFinder, cancellationToken);
        }

        private async Task ScheduleStartAtEventsAsync(
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<StartAtBookmark>(bookmarkFinder, cancellationToken);
            await ScheduleTriggersAsync<StartAtBookmark>(triggerFinder, cancellationToken);
        }

        private async Task ScheduleTimerEventsAsync(
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<TimerBookmark>(bookmarkFinder, cancellationToken);
            await ScheduleTriggersAsync<TimerBookmark>(triggerFinder, cancellationToken);
        }

        private async Task ScheduleCronEventsAsync(
            ITriggerFinder triggerFinder,
            IBookmarkFinder bookmarkFinder,
            CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<CronBookmark>(bookmarkFinder, cancellationToken);
            await ScheduleTriggersAsync<CronBookmark>(triggerFinder, cancellationToken);
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