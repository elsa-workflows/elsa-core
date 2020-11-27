using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa()
                            .AddConsoleActivities()
                            .AddTimerActivities()
                            .AddWorkflow<RecurringTaskWorkflow>()
                            .AddWorkflow<CronTaskWorkflow>()
                            .AddWorkflow(new OneOffWorkflow(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5))));
                    });
    }
}