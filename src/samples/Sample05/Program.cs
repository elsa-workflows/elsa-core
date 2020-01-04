using System;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample05
{
    /// <summary>
    /// A workflow built using a fluent API and registered with DI and run from the workflow host.
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
            await host.RunAsync<HelloWorldWorkflow>();
        }
    }
}