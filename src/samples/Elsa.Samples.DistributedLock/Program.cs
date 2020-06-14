using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using System.Threading.Tasks;
using Elsa.DistributedLocking.Redis;

namespace Elsa.Samples.DistributedLock
{
    internal static class Program
    {
        private static async Task Main()
        {
            await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa(options =>
                            options
                            .UseMongoDbWorkflowStores("Elsa_Samples_Timers", "mongodb://localhost")
                            .UseRedisLockProvider("redis-16642.xx.redislabs.com:16642,password=xxxxxx"))
                        .AddTimerActivities(options =>
                            options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(5)))
                        .AddWorkflow<RecurringWorkflow>();
                });
    }
}