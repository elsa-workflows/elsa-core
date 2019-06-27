using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Extensions;
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
                .AddConsoleActivities()
                .BuildServiceProvider();

            // Define a workflow.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            var workflowDefinition = workflowBuilder
                .StartWith<WriteLine>(x => x.TextExpression = new PlainTextExpression("Hello world!"))
                .Then<WriteLine>(x => x.TextExpression = new PlainTextExpression("Goodbye cruel world..."))
                .Build();

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflowDefinition);

            Console.ReadLine();
        }
    }
}