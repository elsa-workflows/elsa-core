using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Activities;
using Elsa.Activities.Timers.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Timers.HostedServices
{
    public class TimersHostedService : BackgroundService
    {
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IOptions<TimersOptions> options;
        private readonly ILogger<TimersHostedService> logger;

        public TimersHostedService(IWorkflowInvoker workflowInvoker, IOptions<TimersOptions> options, ILogger<TimersHostedService> logger)
        {
            this.workflowInvoker = workflowInvoker;
            this.options = options;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await workflowInvoker.TriggerAsync(nameof(TimerEvent), Variables.Empty, stoppingToken);
                    await workflowInvoker.TriggerAsync(nameof(CronEvent), Variables.Empty, stoppingToken);
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