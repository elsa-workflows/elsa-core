using Elsa.Persistence.YesSql;
using Elsa.Samples.BreakLoop.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.BreakLoop
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
                                .AddActivitiesFrom<Program>()
                                .AddWorkflowsFrom<Program>().StartWorkflow<BreakoutWorkflow>()
                            );
                    });
    }
}