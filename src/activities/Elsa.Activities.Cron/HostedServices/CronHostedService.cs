using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Cron.Activities;
using Elsa.Activities.Cron.Options;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Cron.HostedServices
{
    public class CronHostedService : BackgroundService
    {
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IOptions<CronOptions> options;
        private readonly ILogger<CronHostedService> logger;

        public CronHostedService(IWorkflowInvoker workflowInvoker, IOptions<CronOptions> options, ILogger<CronHostedService> logger)
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
                    await workflowInvoker.TriggerAsync(nameof(CronTrigger), Variables.Empty, stoppingToken);
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