using Elsa.Samples.WhileLoopWorker.Services;
using Elsa.Samples.WhileLoopWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.WhileLoopWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa(options => options.UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                            .AddConsoleActivities()
                            .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
                            .AddSingleton<PhoneCallService>()
                            .AddHostedService<PhoneCallWorker>()
                            .AddWorkflow<PhoneCallWorkflow>();
                    });
    }
}