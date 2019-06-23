using System;
using System.Threading.Tasks;
using Elsa.Core.Extensions;
using Elsa.Services;
using Elsa.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;

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
                .BuildServiceProvider();

            // Create a workflow.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            var workflow = workflowBuilder.Build<CalculatorWorkflow>();

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflow);

            Console.ReadLine();
        }
    }
}