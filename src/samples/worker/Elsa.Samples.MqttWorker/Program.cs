using Elsa.Activities.Mqtt.Extensions;
using Elsa.Samples.MqttWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.MqttWorker
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
                            .AddMqttActivities()
                            .AddWorkflow<ConsumerWorkflow>()
                            .AddWorkflow<ProducerWorkflow>()
                            );
                });
    }
}