using Elsa.Activities.Rebus.Extensions;
using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Elsa.Rebus.AzureServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Rebus.Logging;
using YesSql.Provider.Sqlite;

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
                        .AddElsa(option => option.UseAzureServiceBus("Endpoint=sb://elsa-workflows-2.servicebus.windows.net/;SharedAccessKeyName=Elsa;SharedAccessKey=hAIa+fFuUbHi94y1Z0uO/2UTccjN/y4W0xvpaUd/cr4=", LogLevel.Debug))
                        //.UseRabbitMq("amqp://localhost"))
                        .AddConsoleActivities()
                        .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
                        .AddRebusActivities<Greeting>()
                        //.AddHostedService<Sender>()
                    .AddWorkflow<ProducerWorkflow>()
                    .AddWorkflow<ConsumerWorkflow>();
                });
    }
}