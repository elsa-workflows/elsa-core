using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Samples.AzureServiceBusWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using YesSql.Provider.Sqlite;

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
                        .AddElsa()
                        .AddConsoleActivities()
                        .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
                        .AddAzureServiceBusActivities(options => options.ConnectionString = "Endpoint=sb://elsa-workflows-2.servicebus.windows.net/;SharedAccessKeyName=Elsa;SharedAccessKey=hAIa+fFuUbHi94y1Z0uO/2UTccjN/y4W0xvpaUd/cr4=")
                        .AddWorkflow<ProducerWorkflow>()
                        .AddWorkflow<ConsumerWorkflow>();
                });
    }
}