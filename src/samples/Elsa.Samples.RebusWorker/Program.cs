using Elsa.Activities.Rebus.Extensions;
using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Elsa.ServiceBus.AzureServiceBus;
using Elsa.ServiceBus.RabbitMq.Extensions;
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
                        .AddElsa(option => option
                            .UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared"))
                            .UseAzureServiceBus("Endpoint=sb://elsa-workflows.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=n4NBTw9eSX12AG5BdIkyxCRroJGvh+EMOOM8ypWxWrQ=", LogLevel.Debug))
                            //.UseRabbitMq("amqp://localhost"))
                        .AddConsoleActivities()
                        .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
                        .AddRebusActivities<Greeting>()
                        .AddWorkflow<ProducerWorkflow>()
                        .AddWorkflow<ConsumerWorkflow>();
                });
    }
}