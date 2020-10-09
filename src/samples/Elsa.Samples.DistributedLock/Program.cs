using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using System.Threading.Tasks;
using Elsa.DistributedLocking.AzureBlob;
using System;

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
                                .UseAzureBlobLockProvider("UseDevelopmentStorage=true"))
                            //.UseRedisLockProvider("redis-16642.xx.redislabs.com:16642,password=xxxxxx"))

                        .AddConsoleActivities()
                        .AddSingleton(Console.In)
                        .AddTimerActivities(options =>
                            options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(5)))
                        .AddWorkflow<RecurringWorkflow>();
                });
    }
}