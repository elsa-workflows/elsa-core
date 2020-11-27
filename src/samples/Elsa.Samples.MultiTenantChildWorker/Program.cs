using Elsa.Samples.MultiTenantChildWorker.Messages;
using Elsa.Samples.MultiTenantChildWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Samples.MultiTenantChildWorker
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
                            .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
                            .AddConsoleActivities()
                            .AddRebusActivities<OrderReceived>()
                            .AddWorkflow<GenerateOrdersWorkflow>()
                            .AddWorkflow<OrderReceivedWorkflow>()
                            .AddWorkflow<Tenant1ChildWorkflow>()
                            .AddWorkflow<Tenant2ChildWorkflow>()
                            ;
                    });
    }
}