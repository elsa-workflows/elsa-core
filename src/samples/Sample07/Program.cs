using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample07
{
    /// <summary>
    /// Demonstrates a flowchart-style workflow, where activities are connected to other activities via their outcomes.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddWorkflow<MyWorkflow>() // Register the workflow.
                .BuildServiceProvider();

            // Get a workflow host.
            var host = services.GetRequiredService<IWorkflowHost>();

            // Run the workflow.
            await host.RunAsync<MyWorkflow>();
        }
    }
}