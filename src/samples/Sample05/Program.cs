using System;
using System.Threading.Tasks;
using Elsa.Activities.Cron.Extensions;
using Elsa.Core.Extensions;
using Elsa.Core.Persistence.Extensions;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Sample05
{
    /// <summary>
    /// A minimal workflows program defined in code with a strongly-typed workflow class.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                await host.StartAsync();
                SetupWorkflow(host.Services);
                await host.WaitForShutdownAsync();
            }
        }

        private static void SetupWorkflow(IServiceProvider services)
        {
            // Create a workflow.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            var workflow = workflowBuilder.Build<RecurringWorkflow>();

            // Register the workflow.
            var registry = services.GetService<IWorkflowRegistry>();
            registry.RegisterWorkflow(workflow);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWorkflows()
                .AddCronActivities(options => options.Configure(x => x.SweepInterval = Period.FromSeconds(10)))
                .AddMemoryWorkflowDefinitionStore()
                .AddMemoryWorkflowInstanceStore();
        }
    }
}