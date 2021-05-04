using System.Threading.Tasks;
using Hangfire;
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
                    (context, services) =>
                    {
                        services
                            .AddElsa(options => options
                                .AddConsoleActivities()
                                .AddHangfireTemporalActivities(hangfire => hangfire.UseSqlServerStorage("Server=(localdb)\\MSSQLLocalDB;Database=ElsaHangfire;Trusted_Connection=True;MultipleActiveResultSets=true"))
                                .AddWorkflow<RecurringTaskWorkflow>()
                                .AddWorkflow<CronTaskWorkflow>()
                                .AddWorkflow<CancelTimerWorkflow>()
                                .AddWorkflow(new OneOffWorkflow(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5)))));
                    });
    }
}