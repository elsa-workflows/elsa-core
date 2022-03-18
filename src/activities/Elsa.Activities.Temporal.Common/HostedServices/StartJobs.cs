using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Messages;
using Elsa.HostedServices;
using Elsa.Services;
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

        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ICommandSender _commandSender;
        private readonly ILogger<StartJobs> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public StartJobs(
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IDistributedLockProvider distributedLockProvider,
            ICommandSender commandSender,
            ILogger<StartJobs> logger)
        {
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
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
            await ScheduleBookmarksAsync<StartAtBookmark>(cancellationToken);
            await ScheduleTriggersAsync<StartAtBookmark>(cancellationToken);
        }

        private async Task ScheduleTimerEventsAsync(CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<TimerBookmark>(cancellationToken);
            await ScheduleTriggersAsync<TimerBookmark>(cancellationToken);
        }

        private async Task ScheduleCronEventsAsync(CancellationToken cancellationToken)
        {
            await ScheduleBookmarksAsync<CronBookmark>(cancellationToken);
            await ScheduleTriggersAsync<CronBookmark>(cancellationToken);
        }

        private async Task ScheduleTriggersAsync<T>(CancellationToken cancellationToken) where T : IBookmark
        {
            var stream = _triggerFinder.StreamTriggersByTypeAsync(typeof(T).GetSimpleAssemblyQualifiedName(), TenantId, cancellationToken);
            var index = 0;

            await foreach (var result in stream.WithCancellation(cancellationToken))
            {
                await _commandSender.SendAsync(new ScheduleTemporalTrigger(result), cancellationToken: cancellationToken);
                _logger.LogDebug("Dispatched scheduling trigger {CurrentTriggerCount}", ++index);
            }
        }

        private async Task ScheduleBookmarksAsync<T>(CancellationToken cancellationToken) where T : IBookmark
        {
            var stream = _bookmarkFinder.StreamBookmarksByTypeAsync(typeof(T).GetSimpleAssemblyQualifiedName(), TenantId, cancellationToken);
            var index = 0;

            await foreach (var result in stream.WithCancellation(cancellationToken))
            {
                await _commandSender.SendAsync(new ScheduleTemporalBookmark(result), cancellationToken: cancellationToken);
                _logger.LogDebug("Dispatched scheduling bookmark {CurrentBookmarkIndex} - {BookmarkId}", ++index, result.Id);
            }
        }
    }
}