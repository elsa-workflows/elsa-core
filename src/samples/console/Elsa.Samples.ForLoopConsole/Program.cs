using System.Threading.Tasks;

using Elsa.Persistence.InMemory;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.ForLoopConsole
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
                .AddElsa()
                .AddConsoleActivities()
                .AddWorkflow<LoopingWorkflow>()
                .BuildServiceProvider();
            
            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();
            
            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

            // Execute the workflow.
            await workflowRunner.RunWorkflowAsync<LoopingWorkflow>();
        }
    }
}