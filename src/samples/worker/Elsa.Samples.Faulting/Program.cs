using Elsa.Persistence.YesSql;
using Elsa.Samples.Faulting.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.Faulting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services
                        .AddElsa(options => options
                            .UseYesSqlPersistence()
                            .AddConsoleActivities()
                            .AddQuartzTemporalActivities()
                            .AddWorkflow<FaultyWorkflow>());
                });
    }
}