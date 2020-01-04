using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample06
{
    /// <summary>
    /// Same as Sample05, but this time running the workflow by its ID.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddWorkflow<HelloWorldWorkflow>() // Register the workflow.
                .BuildServiceProvider();

            // Get a workflow host.
            var host = services.GetRequiredService<IWorkflowHost>();

            // Run the workflow.
            await host.RunAsync("MyWorkflow");
        }
    }
}