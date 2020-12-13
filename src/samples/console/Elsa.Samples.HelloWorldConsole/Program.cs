using System;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.YesSql;
using Elsa.Services;
using Elsa.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.HelloWorldConsole
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options.UseYesSqlPersistence())
                .AddConsoleActivities()
                .AddWorkflow<HelloWorld>()
                .AddAutoMapperProfiles<Program>()
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();
            
            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

            // Run the workflow.
            await workflowRunner.RunWorkflowAsync<HelloWorld>();

            var store = services.GetRequiredService<IWorkflowInstanceStore>();
            
            var results = await store.ListAsync(
                new WorkflowDefinitionSpecification(nameof(HelloWorld)).WithTenant("1"), 
                GroupingSpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt),
                PagingSpecification.Page(1, 2));
            
            var count = await store.CountAsync(
                new WorkflowDefinitionSpecification(nameof(HelloWorld)).WithTenant("1"), 
                GroupingSpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt));

            Console.WriteLine(count);
            
            foreach (var result in results)
            {
                Console.WriteLine(result.CreatedAt);
            }
        }
    }
}