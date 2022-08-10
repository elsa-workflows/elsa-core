using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Multitenancy;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RebusWorker
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
                                .UseNonPooledEntityFrameworkPersistence(ef =>
                                {
                                    ef.UseSqlite(
                                        "Data Source=elsa.db;Cache=Shared",
                                        db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                                })
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddRebusActivities<Greeting>()
                                .AddWorkflow<ProducerWorkflow>()
                                .AddWorkflow<ConsumerWorkflow>());

                    builder.Populate(sc);
                });
    }
}