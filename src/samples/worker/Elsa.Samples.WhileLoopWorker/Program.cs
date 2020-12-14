using Elsa.Samples.WhileLoopWorker.Services;
using Elsa.Samples.WhileLoopWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elsa.Activities.Timers;

namespace Elsa.Samples.WhileLoopWorker
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (_, services) =>
                    {
                        services
                            .AddElsa()
                            .AddConsoleActivities()
                            .AddTimerActivities(options => options.UseQuartzProvider())
                            .AddSingleton<PhoneCallService>()
                            .AddHostedService<PhoneCallWorker>()
                            .AddWorkflow<PhoneCallWorkflow>();
                    });
    }
}