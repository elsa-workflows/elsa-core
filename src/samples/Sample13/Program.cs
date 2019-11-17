using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Sample13
{
    /// <summary>
    /// A strongly-typed workflows program demonstrating scripting, and branching.
    /// </summary>
    internal class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddSingleton(Console.In)
                .AddMediatR(typeof(Program))
                .BuildServiceProvider();

            // Create a workflow.
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var workflow = workflowFactory.CreateWorkflow<ActivityOutputWorkflow>();

            // Start the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.StartAsync(workflow);

            Console.ReadLine();
        }
    }
}