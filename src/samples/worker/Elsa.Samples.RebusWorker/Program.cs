using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elsa.Activities.Timers;

namespace Elsa.Samples.RebusWorker
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services
                        .AddElsa()
                        .AddConsoleActivities()
                        .AddTimerActivities(options => options.UseQuartzProvider())
                        .AddRebusActivities<Greeting>()
                        .AddWorkflow<ProducerWorkflow>()
                    .AddWorkflow<ConsumerWorkflow>();
                });
    }
}