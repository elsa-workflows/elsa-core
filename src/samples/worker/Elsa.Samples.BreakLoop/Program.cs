using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Multitenancy;
using Elsa.Persistence.YesSql;
using Elsa.Samples.BreakLoop.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.BreakLoop
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container)))
                .ConfigureServices((_, services) => services.AddElsaServices())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    var sc = new ServiceCollection();

                    builder
                       .ConfigureElsaServices(sc,
                            options => options
                                .UseYesSqlPersistence()
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddActivitiesFrom<Program>()
                                .AddWorkflowsFrom<Program>().StartWorkflow<BreakoutWorkflow>());

                    builder.Populate(sc);
                });
    }
}