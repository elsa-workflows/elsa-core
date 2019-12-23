using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample24
{
    /// <summary>
    /// Demonstrates a coded workflow using inline C# code.
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
            var workflow = workflowFactory.CreateWorkflow<CalculatorWorkflow>();
            
            // Run the workflow.
            var runner = services.GetService<IWorkflowRunner>();
            await runner.RunAsync(workflow);
        }
    }
}