using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using System.Threading.Tasks;
using Elsa.DistributedLocking.AzureBlob;
using System;
using Elsa.DistributedLocking.Redis;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.DistributedLock
{
    /// <summary>
    /// Demonstrates the use of a distributed locking provider.
    /// Make sure that you have a Redis host installed or use the docker-compose.yaml file to run it on Docker.
    /// </summary>
    internal static class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa(options => options
                                .UsePersistence(db => db.UseSqLite("Data Source=elsa2.db;Cache=Shared"))
                                .UseRedisLockProvider("localhost:6379,abortConnect=false"))
                            .AddConsoleActivities()
                            .AddTimerActivities(options => options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(5)))
                            .AddWorkflow<RecurringWorkflow>();
                    });
    }
}