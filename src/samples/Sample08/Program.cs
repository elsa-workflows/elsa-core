using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample08
{
    /// <summary>
    /// Demonstrates a sequential-style workflow, where activities are automatically executed in sequence.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddWorkflow<MySequentialWorkflow>() // Register the workflow.
                .BuildServiceProvider();

            // Get a workflow host.
            var host = services.GetRequiredService<IWorkflowHost>();

            // Run the workflow.
            await host.RunAsync<MySequentialWorkflow>();
        }
    }
}