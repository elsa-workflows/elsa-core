using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Elsa.Activities.Timers;

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
                            .AddElsa()
                            .AddConsoleActivities()
                            .AddTimerActivities(options => options.UseQuartzProvider())
                            .AddWorkflow<RecurringTaskWorkflow>()
                            .AddWorkflow<CronTaskWorkflow>()
                            .AddWorkflow(new OneOffWorkflow(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5))));
                    });
    }
}