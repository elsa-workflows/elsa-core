using Elsa.Samples.RunChildWorkflowWorker.HostedServices;
using Elsa.Samples.RunChildWorkflowWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RunChildWorkflowWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa()
                            .AddConsoleActivities()
                            .AddHostedService<RunParentWorkflow>()
                            .AddWorkflow<ParentWorkflow>()
                            .AddWorkflow<ChildWorkflow>();
                    });
    }
}