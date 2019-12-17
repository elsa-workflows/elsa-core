using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Startup.Activities;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Startup.HostedServices
{
    public class StartupHostedService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<StartupHostedService> logger;

        public StartupHostedService(
            IServiceProvider serviceProvider,
            ILogger<StartupHostedService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
                    await workflowInvoker.TriggerAsync(nameof(Activities.Startup), Variables.Empty, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Exception occurred while invoking startup workflows.");
            }

            await Task.Delay(1, stoppingToken);

        }
    }
}