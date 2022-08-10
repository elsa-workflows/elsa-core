using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container)))
                .ConfigureServices((_, services) => services.AddElsaServices().AddRedis("localhost:6379,abortConnect=false"))
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    var sc = new ServiceCollection();

                    builder
                       .ConfigureElsaServices(sc,
                            options => options
                                .ConfigureDistributedLockProvider(options => options.UseRedisLockProvider())
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddWorkflow<RecurringWorkflow>());

                    builder.Populate(sc);
                });
    }
}