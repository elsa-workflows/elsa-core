using Elsa.Persistence.YesSql;
using Elsa.Samples.WhileLoopWorker.Services;
using Elsa.Samples.WhileLoopWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                            .AddElsa(options => options.UseYesSqlPersistence()
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddActivity<MakePhoneCall>()
                                .AddWorkflow<PhoneCallWorkflow>())
                            .AddSingleton<PhoneCallService>()
                            .AddHostedService<PhoneCallWorker>();
                    });
    }
}