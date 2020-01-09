using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Models;
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

            var registry = services.GetRequiredService<IWorkflowRegistry>();
            
            // Get a workflow host.
            var host = services.GetRequiredService<IWorkflowHost>();

            // Run the workflow.
            await host.RunAsync<MyWorkflow>();
            await host.RunAsync<MyWorkflow>();
        }
    }
}