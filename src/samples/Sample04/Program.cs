using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample04.Activities;

namespace Sample04
{
    /// <summary>
    /// A strongly-typed workflows program demonstrating scripting, and branching.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddSingleton(Console.In)
                .AddElsa()
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
                var activity = workflow.Definition.Activities.First(x => x.Id == logEntry.ActivityId);
                Console.WriteLine("{0}: {1}", logEntry.Timestamp, activity.DisplayName);
            }
            Console.ReadLine();
        }
    }
}