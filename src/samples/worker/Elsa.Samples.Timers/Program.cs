using System.Threading.Tasks;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.Timers.Activities;
using Elsa.Samples.Timers.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Samples.Timers
{
    internal static class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (_, services) =>
                    {
                        services
                            .AddElsa(options => options
                                    .UseEntityFrameworkPersistence(ef => ef
                                        .UseSqlite("Data Source=elsa.sqlite.db;Cache=Shared", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)), true)
                                    .AddConsoleActivities()
                                    .AddQuartzTimerActivities()
                                    .AddActivity<MyContainer1>()
                                    .AddActivity<MyContainer2>()
                                    .AddWorkflow<SingletonTimerWorkflow>()
                                    //.AddWorkflow<TimerWorkflow>()
                                //.AddWorkflow<ComplicatedWorkflow>()
                                //.AddWorkflow<CancelTimerWorkflow>()
                                //.AddWorkflow<CronTaskWorkflow>()
                                //.AddWorkflow(sp => ActivatorUtilities.CreateInstance<OneOffWorkflow>(sp, sp.GetRequiredService<IClock>().GetCurrentInstant().Plus(Duration.FromSeconds(5))))
                            )
                            //.StartWorkflow<ComplicatedWorkflow>()
                            ;
                    });
    }
}