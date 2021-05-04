using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Elsa.Samples.DistributedLock
{
    /// <summary>
    /// Demonstrates the use of a distributed locking provider.
    /// Make sure that you have a Redis host installed or use the docker-compose.yaml file to run it on Docker.
    /// </summary>
    internal static class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (_, services) =>
                    {
                        services
                            .AddElsa(elsa => elsa.ConfigureDistributedLockProvider(options => options.UseRedisLockProvider("localhost:6379,abortConnect=false"))
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddWorkflow<RecurringWorkflow>());
                    });
    }
}