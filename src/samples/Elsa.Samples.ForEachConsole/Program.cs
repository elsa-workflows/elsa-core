using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.ForEachConsole
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
            
            // Get the workflow host.
            var workflowHost = services.GetService<IWorkflowHost>();

            // Execute the workflow.
            await workflowHost.RunWorkflowAsync<LoopingWorkflow>();
        }
    }
}