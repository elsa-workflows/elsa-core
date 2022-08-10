using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Samples.RunChildWorkflowWorker.HostedServices;
using Elsa.Samples.RunChildWorkflowWorker.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RunChildWorkflowWorker
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
                                .AddConsoleActivities()
                                .AddWorkflow<ParentWorkflow>()
                                .AddWorkflow<ChildWorkflow>())
                       .AddHostedService<RunParentWorkflow>();

                    builder.Populate(sc);
                });
    }
}