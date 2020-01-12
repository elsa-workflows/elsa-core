using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Options;
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
        private readonly IOptions<TimersOptions> options;
        private readonly ILogger<TimersHostedService> logger;

        public TimersHostedService(
            IServiceProvider serviceProvider,
            IOptions<TimersOptions> options, 
            ILogger<TimersHostedService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.options = options;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowHost>(); 
                    await workflowInvoker.TriggerAsync(nameof(TimerEvent), cancellationToken: stoppingToken);
                    await workflowInvoker.TriggerAsync(nameof(CronEvent), cancellationToken:stoppingToken);
                    await workflowInvoker.TriggerAsync(nameof(InstantEvent), cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Exception occurred while invoking workflows.");
                }

                await Task.Delay(options.Value.SweepInterval.ToDuration().ToTimeSpan(), stoppingToken);
            }
        }
    }
}