using Elsa.Persistence.InMemory;
using Elsa.Samples.CustomAttributesChildWorker.Messages;
using Elsa.Samples.CustomAttributesChildWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.CustomAttributesChildWorker
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
                            .AddElsaPersistenceInMemory()
                            .AddTimerActivities()
                            .AddConsoleActivities()
                            .AddRebusActivities<OrderReceived>()
                            .AddWorkflow<GenerateOrdersWorkflow>()
                            .AddWorkflow<OrderReceivedWorkflow>()
                            .AddWorkflow<Customer1Workflow>()
                            .AddWorkflow<Customer2Workflow>()
                            ;
                    });
    }
}