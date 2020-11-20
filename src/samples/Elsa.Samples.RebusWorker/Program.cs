using Elsa.Activities.Rebus.Extensions;
using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
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
                            .UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                        .AddConsoleActivities()
                        .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
                        .AddRebusActivities<Greeting>()
                        .AddWorkflow<ProducerWorkflow>()
                        .AddWorkflow<ConsumerWorkflow>();
                });
    }
}