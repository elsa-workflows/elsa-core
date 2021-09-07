using Elsa.Samples.WatchDirectoryWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Elsa.Samples.WatchDirectoryWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa(options => options
                            .AddConsoleActivities()
                            .AddFileActivities()
                            .AddWorkflow<WatchDirectoryCreatedWorkflow>()
                            .AddWorkflow<WatchDirectoryChangedWorkflow>()
                            .AddWorkflow<WatchDirectoryDatWorkflow>()
                            .AddWorkflow<WatchDirectoryDeletedWorkflow>());
                });
    }
}
