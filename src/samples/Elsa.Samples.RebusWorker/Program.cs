using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Elsa.Rebus.AzureServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Logging;

namespace Elsa.Samples.RebusWorker
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
                        .AddElsa(option => option.UseAzureServiceBus(hostContext.Configuration.GetConnectionString("AzureServiceBus"), LogLevel.Debug))
                        //.UseRabbitMq("amqp://localhost"))
                        .AddConsoleActivities()
                        .AddTimerActivities()
                        .AddRebusActivities<Greeting>()
                        //.AddHostedService<Sender>()
                    .AddWorkflow<ProducerWorkflow>()
                    .AddWorkflow<ConsumerWorkflow>();
                });
    }
}