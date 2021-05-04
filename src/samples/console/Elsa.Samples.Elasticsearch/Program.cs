using System.Threading.Tasks;

using Elsa.Services;
using Elsa.Indexing.Extensions;

using Microsoft.Extensions.DependencyInjection;
using System;
using Elsa.Indexing.Services;

namespace Elsa.Samples.Elasticsearch
{
    /// <summary>
    /// Demonstrates the use of Elasticsearch.
    /// </summary>
    internal class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddWorkflow<HelloWorld>()
                    .UseElasticsearch(configure =>
                    {
                        configure.Uri = new Uri[] { new Uri("http://localhost:9200/") };
                    })
                )
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<HelloWorld>();

            var instanceSearcher = services.GetRequiredService<IWorkflowInstanceSearch>();

            var instances = await instanceSearcher.SearchAsync("HelloWorld");

            Console.WriteLine($"{instances.Count} instances were found");
        }
    }
}
