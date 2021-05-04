using System;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Persistence.YesSql;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.HelloWorldConsole
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
                .AddAutoMapperProfiles<Program>()
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<HelloWorld>();

            var store = services.GetRequiredService<IWorkflowInstanceStore>();

            var results = await store.FindManyAsync(
                new WorkflowDefinitionIdSpecification(nameof(HelloWorld)),
                OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt),
                Paging.Page(1, 2));

            var count = await store.CountAsync(new WorkflowDefinitionIdSpecification(nameof(HelloWorld)));

            Console.WriteLine(count);

            foreach (var result in results)
            {
                Console.WriteLine(result.CreatedAt);
            }
        }
    }
}