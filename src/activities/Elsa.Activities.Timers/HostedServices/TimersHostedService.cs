using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Triggers;
using Elsa.DistributedLock;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Elsa.Activities.Timers.HostedServices
{
    public class TimersHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IClock _clock;
        private readonly IOptions<TimersOptions> _options;
        private readonly ILogger<TimersHostedService> _logger;

        public TimersHostedService(
            IServiceProvider serviceProvider,
            IDistributedLockProvider distributedLockProvider,
            IClock clock,
            IOptions<TimersOptions> options,
            ILogger<TimersHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _distributedLockProvider = distributedLockProvider;
            _clock = clock;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (await _distributedLockProvider.AcquireLockAsync(nameof(TimersHostedService), stoppingToken))
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var workflowScheduler = scope.ServiceProvider.GetRequiredService<IWorkflowScheduler>();
                        var now = _clock.GetCurrentInstant();
                        await workflowScheduler.TriggerWorkflowsAsync<TimerEventTrigger>(x => x.ExecuteAt <= now, cancellationToken: stoppingToken);
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(CronEvent), cancellationToken: stoppingToken);
                        await workflowScheduler.TriggerWorkflowsAsync<InstantEventTrigger>(x => x.Instant <= now, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Exception occurred while invoking workflows.");
                    }
                    finally
                    {
                        await _distributedLockProvider.ReleaseLockAsync(GetType().Name, stoppingToken);
                    }
                }

                await Task.Delay(_options.Value.SweepInterval.ToTimeSpan(), stoppingToken);
            }
        }
    }
}