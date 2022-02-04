using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Elsa.Samples.ReadLineToFile
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddElsa(configure => configure
                    .AddConsoleActivities()
                    .AddFileActivities())
                .AddAutoMapperProfiles<Program>()
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<ReadLineToFileWorkflow>();

            var store = services.GetRequiredService<IWorkflowInstanceStore>();

            var results = await store.FindManyAsync(
                new WorkflowDefinitionIdSpecification(nameof(ReadLineToFileWorkflow)),
                OrderBySpecification.OrderByDescending<WorkflowInstance>(x => x.CreatedAt),
                Paging.Page(1, 2));

            var count = await store.CountAsync(new WorkflowDefinitionIdSpecification(nameof(ReadLineToFileWorkflow)));

            Console.WriteLine(count);

            foreach (var result in results)
            {
                Console.WriteLine(result.CreatedAt);
            }
        }
    }
}
