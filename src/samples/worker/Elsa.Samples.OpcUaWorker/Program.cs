using Elsa.Activities.OpcUa.Extensions;
using Elsa.Samples.OpcUaWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.OpcUaWorker
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
                            .AddOpcUaActivities()
                            .AddWorkflow<ConsumerWorkflow>()
                            //.AddWorkflow<ProducerWorkflow>()
                            //.StartWorkflow<SendAndReceiveWorkflow>()
                            );
                });
    }
}