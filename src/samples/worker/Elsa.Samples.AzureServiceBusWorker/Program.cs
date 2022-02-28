using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.AzureServiceBusWorker.Workflows;
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
                            .UseEntityFrameworkPersistence(db => db.UseSqlite("Data Source=elsa.efcore.db;Cache=Shared"))
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