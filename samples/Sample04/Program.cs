using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.Memory;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample04.Activities;

namespace Sample04
{
    /// <summary>
    /// A strongly-typed workflows program demonstrating scripting, and branching.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflows()
                .AddMemoryWorkflowInstanceStore()
                .AddConsoleActivities()
                .AddActivity<Sum>()
                .AddActivity<Subtract>()
                .AddActivity<Multiply>()
                .AddActivity<Divide>()
                .BuildServiceProvider();

            // Create a workflow.
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var workflow = workflowFactory.CreateWorkflow<CalculatorWorkflow>();

            // Start the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.StartAsync(workflow);

            Console.WriteLine("Workflow has ended. Here are the activities that have executed:");
            foreach (var logEntry in workflow.ExecutionLog)
            {
                Console.WriteLine("{0}: {1}", logEntry.Timestamp, logEntry.ActivityId);
            }
            Console.ReadLine();
        }
    }
}