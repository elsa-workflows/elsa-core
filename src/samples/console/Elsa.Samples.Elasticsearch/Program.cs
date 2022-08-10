using System;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Indexing.Extensions;
using Elsa.Indexing.Services;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

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
            var serviceCollection = new ServiceCollection().AddElsaServices();

            var services = MultitenantContainerFactory.CreateSampleMultitenantContainer(serviceCollection,
                options => options
                    .AddConsoleActivities()
                    .AddWorkflow<HelloWorld>()
                    .UseElasticsearch(configure =>
                    {
                        configure.Uri = new Uri[] { new Uri("http://localhost:9200/") };
                    }));

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
