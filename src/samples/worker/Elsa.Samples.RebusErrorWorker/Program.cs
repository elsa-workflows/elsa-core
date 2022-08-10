using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RebusErrorWorker;

/// <summary>
/// When this program starts, it will continuously process messages from the error queue and send them to the return address for re-processing.
/// </summary>
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
                       .ConfigureElsaServices(sc)
                       .AddHostedService<ProcessErrorQueue>();

                    builder.Populate(sc);
                });
}