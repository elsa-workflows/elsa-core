using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Multitenancy;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.AzureServiceBusWorker.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.AzureServiceBusWorker
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
                .ConfigureServices((hostContext, services) => services.AddElsaServices())
                .ConfigureContainer<ContainerBuilder>((hostContext, builder) =>
                {
                    var sc = new ServiceCollection();

                    builder
                       .ConfigureElsaServices(sc,
                            options => options
                                .UseEntityFrameworkPersistence(db => db.UseSqlite("Data Source=elsa.efcore.db;Cache=Shared"))
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddAzureServiceBusActivities(o => o.ConnectionString = hostContext.Configuration.GetConnectionString("AzureServiceBus"))
                                .AddWorkflow<ProducerWorkflow>()
                                .AddWorkflow<ConsumerWorkflow>()
                                .StartWorkflow<SendAndReceiveWorkflow>());

                    builder.Populate(sc);
                });
    }
}