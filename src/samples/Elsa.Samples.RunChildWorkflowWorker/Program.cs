using System.Data;
using Elsa.Samples.RunChildWorkflowWorker.HostedServices;
using Elsa.Samples.RunChildWorkflowWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.RunChildWorkflowWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddElsa(options => options.UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared", IsolationLevel.ReadUncommitted)))
                            .AddConsoleActivities()
                            .AddHostedService<RunParentWorkflow>()
                            .AddWorkflow<ParentWorkflow>()
                            .AddWorkflow<ChildWorkflow>();
                    });
    }
}