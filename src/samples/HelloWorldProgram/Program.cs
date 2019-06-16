using System;
using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Builders;
using Elsa.Core.Builders;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Expressions;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HelloWorldProgram
{
    /// <summary>
    /// A minimal workflows program.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflowBuilder()
                .AddWorkflowsCore()
                .AddConsoleActivities()
                .BuildServiceProvider();

            // Create a workflow.
            var workflow = services.GetService<WorkflowBuilder>()
                .Add<WriteLine>(x => x.TextExpression = PlainText.Expression<string>("Hello World!"))
                .Next<WriteLine>(x => x.TextExpression = PlainText.Expression<string>("Goodbye!"))
                .BuildWorkflow();
            
            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            invoker.InvokeAsync(workflow);
            
            // Output: Hello World!

            Console.ReadLine();
        }
    }
}