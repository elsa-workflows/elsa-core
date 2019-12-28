using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample25
{
    /// <summary>
    /// Demonstrates some activities.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();
            
            // Create a workflow.
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var sequenceDemoWorkflow = workflowFactory.CreateWorkflow<SequenceDemoWorkflow>();
            var forLoopDemoWorkflow = workflowFactory.CreateWorkflow<ForLoopDemoWorkflow>();
            
            // Run the workflows.
            var runner = services.GetService<IWorkflowRunner>();
            await runner.RunAsync(sequenceDemoWorkflow);
            await runner.RunAsync(forLoopDemoWorkflow);
        }
    }
}