using Elsa.Activities.RabbitMq.Extensions;
using Elsa.Samples.RabbitMqWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RabbitMqWorker
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
                        .AddElsa(options => options
                            .AddConsoleActivities()
                            .AddQuartzTemporalActivities()
                            .AddRabbitMqActivities()
                            .AddWorkflow<ConsumerWorkflow>()
                            .AddWorkflow<ProducerWorkflow>()
                            .StartWorkflow<SendAndReceiveWorkflow>()
                            );
                });
    }
}