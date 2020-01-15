using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Activities;
using Elsa.Activities.Timers.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
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
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>(); 
                        await workflowInvoker.TriggerAsync(nameof(TimerEvent), Variables.Empty, stoppingToken);
                        await workflowInvoker.TriggerAsync(nameof(CronEvent), Variables.Empty, stoppingToken);
                        await workflowInvoker.TriggerAsync(nameof(InstantEvent), Variables.Empty, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Exception occurred while invoking workflows.");
                }

                await Task.Delay(options.Value.SweepInterval.ToTimeSpan(), stoppingToken);
            }
        }
    }
}