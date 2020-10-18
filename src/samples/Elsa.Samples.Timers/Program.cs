using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa(options => options.UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                            .AddConsoleActivities()
                            .AddTimerActivities(options => options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(5)))
                            .AddWorkflow<RecurringTaskWorkflow>()
                            .AddWorkflow<CronTaskWorkflow>();
                    });
    }
}