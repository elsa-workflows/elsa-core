using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Persistence.YesSql;
using Elsa.Samples.WhileLoopWorker.Activities;
using Elsa.Samples.WhileLoopWorker.Services;
using Elsa.Samples.WhileLoopWorker.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.WhileLoopWorker
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
                                .AddActivity<MakePhoneCall>()
                                .AddWorkflow<PhoneCallWorkflow>())
                       .AddMultiton<PhoneCallService>()
                       .AddHostedService<PhoneCallWorker>();

                    builder.Populate(sc);
                });
    }
}