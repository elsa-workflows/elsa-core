using Elsa.Persistence.InMemory;
using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                        .AddElsa()
                        .AddElsaPersistenceInMemory()
                        .AddConsoleActivities()
                        .AddTimerActivities()
                        .AddRebusActivities<Greeting>()
                        .AddWorkflow<ProducerWorkflow>()
                    .AddWorkflow<ConsumerWorkflow>();
                });
    }
}