using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Persistence.InMemory;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Samples.AzureServiceBusWorker.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.AzureServiceBusWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa(options => options.UseYesSqlPersistence())
                        .AddConsoleActivities()
                        .AddTimerActivities()
                        .AddAzureServiceBusActivities(options => options.ConnectionString = hostContext.Configuration.GetConnectionString("AzureServiceBus"))
                        .AddWorkflow<ProducerWorkflow>()
                        .AddWorkflow<ConsumerWorkflow>();
                });
    }
}