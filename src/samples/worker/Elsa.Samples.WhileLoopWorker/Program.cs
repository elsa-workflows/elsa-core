using Elsa.Samples.WhileLoopWorker.Services;
using Elsa.Samples.WhileLoopWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elsa.Persistence.InMemory;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Samples.WhileLoopWorker.Activities;

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
                            .AddElsa(options => options.UseYesSqlPersistence())
                            .AddConsoleActivities()
                            .AddTimerActivities(options => options.UseQuartzProvider())
                            .AddSingleton<PhoneCallService>()
                            .AddHostedService<PhoneCallWorker>()
                            .AddActivity<MakePhoneCall>()
                            .AddWorkflow<PhoneCallWorkflow>();
                    });
    }
}