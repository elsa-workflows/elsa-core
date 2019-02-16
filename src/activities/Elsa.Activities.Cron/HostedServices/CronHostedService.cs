using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Cron.Activities;
using Elsa.Activities.Cron.Options;
using Elsa.Models;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Cron.HostedServices
{
    public class CronHostedService : BackgroundService
    {
        private readonly IWorkflowHost workflowHost;
        private readonly IOptions<CronOptions> options;
        private readonly ILogger<CronHostedService> logger;

        public CronHostedService(IWorkflowHost workflowHost, IOptions<CronOptions> options, ILogger<CronHostedService> logger)
        {
            this.workflowHost = workflowHost;
            this.options = options;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await workflowHost.TriggerWorkflowAsync(nameof(CronTrigger), Variables.Empty, stoppingToken);
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