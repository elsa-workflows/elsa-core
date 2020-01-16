using System.Threading.Tasks;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main()
        {
            var host = CreateHostBuilder().UseConsoleLifetime().Build();
            await host.Services.StartElsaAsync();
            await host.RunAsync();
        }
        
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa()
                        .AddTimerActivities(options => options.Configure(timer => timer.SweepInterval = Duration.FromSeconds(1)))
                        .AddWorkflow<RecurringTaskWorkflow>();
                });
    }
}