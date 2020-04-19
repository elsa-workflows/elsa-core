using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main()
        {
            await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();
        }
        
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa(options =>
                            options.UseMongoDbWorkflowStores("Elsa_Samples_Timers", "mongodb://localhost"))
                        .AddTimerActivities(options =>
                            options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(5)))
                        .AddWorkflow<RecurringTaskWorkflow>();
                    //.AddWorkflow<CronTaskWorkflow>();
                });
    }
}