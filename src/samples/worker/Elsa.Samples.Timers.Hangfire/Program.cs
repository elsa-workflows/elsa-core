using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Elsa.Activities.Timers.Hangfire.Extensions;
using Hangfire;

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
                            .AddElsa()
                            .AddConsoleActivities()
                            .AddTimerActivities(options => 
                                options.UseHangfireProvider(configure => 
                                    configure.UseSqlServerStorage("Server=(localdb)\\MSSQLLocalDB;Database=ElsaHangfire;Trusted_Connection=True;MultipleActiveResultSets=true")))
                            .AddWorkflow<RecurringTaskWorkflow>()
                            .AddWorkflow<CronTaskWorkflow>()
                            .AddWorkflow(new OneOffWorkflow(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5))));
                    });
    }
}