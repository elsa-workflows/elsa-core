using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Persistence.Memory;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample02
{
    /// <summary>
    /// A minimal workflows program defined in code using fluent workflow builder and Console activities.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflows()
                .AddMemoryWorkflowInstanceStore()
                .AddConsoleActivities()
                .BuildServiceProvider();

            // Define a workflow.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            var workflowDefinition = workflowBuilder
                .StartWith<WriteLine>(x => x.TextExpression = new Literal("Hello world!"))
                .Then<WriteLine>(x => x.TextExpression = new Literal("Goodbye cruel world..."))
                .Build();

            // Run the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.StartAsync(workflowDefinition);

            Console.ReadLine();
        }
    }
}