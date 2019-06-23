using System;
using System.Threading.Tasks;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Services;
using Elsa.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Sample01
{
    /// <summary>
    /// A minimal workflows program defined in code with a strongly-typed workflow class.
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
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var workflowBlueprint = workflowBuilder.Build<HelloWorldWorkflow>();
            var workflow = workflowFactory.CreateWorkflow(workflowBlueprint);

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflow);

            Console.ReadLine();
        }
    }
}