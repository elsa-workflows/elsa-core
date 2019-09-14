using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.Memory;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                .ConfigureLogging(logging => logging.AddConsole())
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                SetupWorkflow(host.Services);
                await host.StartAsync();
                await host.WaitForShutdownAsync();
            }
        }

        private static void SetupWorkflow(IServiceProvider services)
        {
            // Register the workflow.
            var registry = services.GetService<IWorkflowRegistry>();
            registry.RegisterWorkflow<RecurringWorkflow>();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWorkflows()
                .AddConsoleActivities()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Period.FromSeconds(1)))
                .AddMemoryWorkflowDefinitionStore()
                .AddMemoryWorkflowInstanceStore();
        }
    }
}