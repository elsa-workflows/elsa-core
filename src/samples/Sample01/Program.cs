using System;
using System.Threading.Tasks;
using Elsa.Core.Extensions;
using Elsa.Services;
using Elsa.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Sample01
{
    /// <summary>
    /// A minimal workflows program.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflows()
                .BuildServiceProvider();

            // Create a workflow.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            var workflow = workflowBuilder.Build<HelloWorldWorkflow>();

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflow);

            Console.ReadLine();
        }
    }
}