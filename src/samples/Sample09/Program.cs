using System;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample09
{
    /// <summary>
    /// Demonstrates a mix of flowchart-style workflows mixed with sequential activities.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddWorkflow<MyMixedWorkflow>() // Register the workflow.
                .BuildServiceProvider();

            // Get a workflow host.
            var host = services.GetRequiredService<IWorkflowHost>();

            // Run the workflow.
            await host.RunAsync<MyMixedWorkflow>();
            
            // Gather user input.
            var input = Console.ReadLine();
            
            // Resume blocked workflows.
            await host.TriggerAsync(nameof(ReadLine), input);
        }
    }
}