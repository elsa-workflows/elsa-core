using System.Threading.Tasks;
using Elsa.Data.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa(
                                options =>
                                    options.UsePersistence(
                                        config => config.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                            .AddTimerActivities(
                                options =>
                                    options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(5)))
                            .AddWorkflow<RecurringTaskWorkflow>();
                    });
    }
}