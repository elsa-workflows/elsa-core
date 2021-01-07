using System.Data;
using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Samples.AzureServiceBusWorker.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YesSql.Provider.Sqlite;

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
                            //.UseYesSqlPersistence(yesSql => yesSql.UseSqLite("Data Source=elsa.yessql.db;Cache=Shared", IsolationLevel.ReadUncommitted))
                            .UseEntityFrameworkPersistence(db => db.UseSqlite("Data Source=elsa.efcore.db;Cache=Shared", sqlite => sqlite.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)), true)
                            .AddConsoleActivities()
                            .AddQuartzTimerActivities()
                            .AddAzureServiceBusActivities(o => o.ConnectionString = hostContext.Configuration.GetConnectionString("AzureServiceBus"))
                            .AddWorkflow<ProducerWorkflow>()
                            .AddWorkflow<ConsumerWorkflow>());
                });
    }
}