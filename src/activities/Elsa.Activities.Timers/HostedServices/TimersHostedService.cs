using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Options;
using Elsa.DistributedLock;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Timers.HostedServices
{
    public class TimersHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IOptions<TimersOptions> _options;
        private readonly ILogger<TimersHostedService> _logger;

        public TimersHostedService(
            IServiceProvider serviceProvider,
            IDistributedLockProvider distributedLockProvider,
            IOptions<TimersOptions> options,
            ILogger<TimersHostedService> logger)
        {
            this._serviceProvider = serviceProvider;
            this._distributedLockProvider = distributedLockProvider;
            this._options = options;
            this._logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (await _distributedLockProvider.AcquireLockAsync(GetType().Name, stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var workflowScheduler = scope.ServiceProvider.GetRequiredService<IWorkflowScheduler>();
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(TimerEvent), cancellationToken: stoppingToken);
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(CronEvent), cancellationToken: stoppingToken);
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(InstantEvent), cancellationToken: stoppingToken);
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