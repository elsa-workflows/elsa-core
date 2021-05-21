using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.RebusWorker.Messages;
using Elsa.Samples.RebusWorker.Workflows;
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
                .ConfigureServices((_, services) =>
                {
                    services
                        .AddElsa(options => options
                            // Configure Elsa to use the Entity Framework Core persistence provider using Sqlite.
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
                });
    }
}