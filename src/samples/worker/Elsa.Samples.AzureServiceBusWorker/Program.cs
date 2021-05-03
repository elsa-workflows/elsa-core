using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.AzureServiceBusWorker.Workflows;
using Microsoft.EntityFrameworkCore;
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
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa(options => options
                            .UseNonPooledEntityFrameworkPersistence(db => db.UseSqlite("Data Source=elsa.efcore.db;Cache=Shared", sqlite => sqlite.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)))
                            .AddConsoleActivities()
                            .AddQuartzTemporalActivities()
                            .AddAzureServiceBusActivities(o => o.ConnectionString = hostContext.Configuration.GetConnectionString("AzureServiceBus"))
                            .AddWorkflow<ProducerWorkflow>()
                            .AddWorkflow<ConsumerWorkflow>()
                            .StartWorkflow<SendAndReceiveWorkflow>()
                            );
                });
    }
}