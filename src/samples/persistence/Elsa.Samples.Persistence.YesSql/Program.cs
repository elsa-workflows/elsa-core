using System;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Persistence.YesSql;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Persistence.YesSql
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .UseYesSqlPersistence()
                    .AddConsoleActivities()
                    .AddWorkflow<HelloWorld>())
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host). This will run things like DB migrations.
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            var runWorkflowResult = await workflowRunner.BuildAndStartWorkflowAsync<HelloWorld>();
            var workflowInstance = runWorkflowResult.WorkflowInstance!;

            // Get a reference to the workflow instance store.
            var store = services.GetRequiredService<IWorkflowInstanceStore>();

            // Count the number of workflow instances of HelloWorld.
            var count = await store.CountAsync(new WorkflowDefinitionIdSpecification(nameof(HelloWorld)));

            Console.WriteLine(count);

             var loadedWorkflowInstance = await store.FindByIdAsync(workflowInstance.Id);
             Console.WriteLine(loadedWorkflowInstance);
        }
    }
}