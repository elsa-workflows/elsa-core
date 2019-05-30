using System;
using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
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
                .AddWorkflowsInvoker()
                .AddConsoleActivities()
                .BuildServiceProvider();

            // Create a workflow invoker.
            var invoker = services.GetService<IWorkflowInvoker>();

            // Create a workflow.
            var workflow = new Workflow();

            // Add a single activity to execute.
            workflow.Activities.Add(new WriteLine("Hello World!"));

            // Invoke the workflow.
            invoker.InvokeAsync(workflow);
            
            // Output: Hello World!

            Console.ReadLine();
        }
    }
}