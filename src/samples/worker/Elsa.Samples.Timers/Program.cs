using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (_, services) =>
                    {
                        services
                            .AddElsa(options => options.UseYesSqlPersistence()
                                .AddConsoleActivities()
                                .AddQuartzTimerActivities()
                                .AddWorkflow<RecurringTaskWorkflow>()
                                .AddActivity<MyContainer1>()
                                .AddActivity<MyContainer2>()
                                //.AddWorkflow<CancelTimerWorkflow>()
                                //.AddWorkflow<CronTaskWorkflow>()
                                //.AddWorkflow(new OneOffWorkflow(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5))))
                            )
                            .StartWorkflow<RecurringTaskWorkflow>();
                    });
    }
}