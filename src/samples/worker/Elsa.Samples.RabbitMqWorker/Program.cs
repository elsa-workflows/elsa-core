using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Activities.RabbitMq.Extensions;
using Elsa.Multitenancy;
using Elsa.Samples.RabbitMqWorker.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RabbitMqWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
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
                                .AddQuartzTemporalActivities()
                                .AddRabbitMqActivities()
                                .AddWorkflow<ConsumerWorkflow>()
                                .AddWorkflow<ProducerWorkflow>()
                                .StartWorkflow<SendAndReceiveWorkflow>());

                    builder.Populate(sc);
                });
    }
}