using System.Threading.Tasks;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.Timers.Activities;
using Elsa.Samples.Timers.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                                .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddActivity<MyContainer1>()
                                .AddActivity<MyContainer2>()
                                //.AddWorkflow<SingletonTimerWorkflow>()
                                //.AddWorkflow<TimerWorkflow>()
                                .AddWorkflow<ComplicatedWorkflow>()
                                //.AddWorkflow<CancelTimerWorkflow>()
                                //.AddWorkflow<CronTaskWorkflow>()
                                //.AddWorkflow(sp => ActivatorUtilities.CreateInstance<OneOffWorkflow>(sp, sp.GetRequiredService<IClock>().GetCurrentInstant().Plus(Duration.FromSeconds(5))))
                                .StartWorkflow<ComplicatedWorkflow>()
                            );
                    });
    }
}