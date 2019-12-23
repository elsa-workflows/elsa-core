using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Persistence;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample15
{
    /// <summary>
    /// A simple demonstration of using the MongoDB persistence providers.
    /// If you don't have MongoDB installed but you do have Docker, run `docker-compose up` to run a container with MongoDB (see the 'docker-compose.yaml' file). 
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            var services = BuildServices();

            // Create a workflow definition.
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = await registry.GetWorkflowDefinitionAsync<HelloWorldWorkflow>();

            // Mark this definition as the "latest" version.
            workflowDefinition.IsLatest = true;
            workflowDefinition.Version = 1;

            using var scope = services.CreateScope();
            
            // Execute the workflow.
            var invoker = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            var executionContext = await invoker.RunAsync(workflowDefinition);

            // Persist the workflow instance.
            var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflowInstance = executionContext.Workflow.ToInstance();
            await instanceStore.SaveAsync(workflowInstance);
        }

        private static IServiceProvider BuildServices()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        ["ConnectionStrings:MongoDb"] = "mongodb://localhost"
                    }
                )
                .Build();

            return new ServiceCollection()
                .AddElsa(x => x.AddMongoDbStores(configuration, "Elsa", "MongoDb"))
                .AddStartupRunner()
                .AddConsoleActivities()
                .AddWorkflow<HelloWorldWorkflow>()
                .BuildServiceProvider();
        }
    }
}