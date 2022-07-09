using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.IfThenElse
{
    /// <summary>
    /// Demonstrates a workflow with a For looping construct.
    /// </summary>
    static class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddWorkflow<IfThenElseWorkflow>())
                .BuildServiceProvider();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Execute the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<IfThenElseWorkflow>();
        }
    }
}