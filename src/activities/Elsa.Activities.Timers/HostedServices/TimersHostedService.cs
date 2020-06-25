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
        private readonly IServiceProvider serviceProvider;
        private readonly IDistributedLockProvider distributedLockProvider;
        private readonly IOptions<TimersOptions> options;
        private readonly ILogger<TimersHostedService> logger;

        public TimersHostedService(
            IServiceProvider serviceProvider,
            IDistributedLockProvider distributedLockProvider,
            IOptions<TimersOptions> options,
            ILogger<TimersHostedService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.distributedLockProvider = distributedLockProvider;
            this.options = options;
            this.logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (await distributedLockProvider.AcquireLockAsync(GetType().Name, stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var workflowScheduler = scope.ServiceProvider.GetRequiredService<IWorkflowScheduler>();
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(TimerEvent), cancellationToken: stoppingToken);
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(CronEvent), cancellationToken: stoppingToken);
                        await workflowScheduler.TriggerWorkflowsAsync(nameof(InstantEvent), cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Exception occurred while invoking workflows.");
                    }
                    finally
                    {
                        await distributedLockProvider.ReleaseLockAsync(GetType().Name, stoppingToken);
                    }
                }

                await Task.Delay(options.Value.SweepInterval.ToTimeSpan(), stoppingToken);
            }
        }
    }
}