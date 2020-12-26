using Elsa.Samples.RunChildWorkflowWorker.HostedServices;
using Elsa.Samples.RunChildWorkflowWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RunChildWorkflowWorker
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (_, services) =>
                    {
                        services
                            .AddElsa(options => options
                                .AddConsoleActivities()
                                .AddWorkflow<ParentWorkflow>()
                                .AddWorkflow<ChildWorkflow>())
                            .AddHostedService<RunParentWorkflow>();
                    });
    }
}