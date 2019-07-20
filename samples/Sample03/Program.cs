using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample03
{
    /// <summary>
    /// A minimal workflows program defined as data (workflow definition) and Console activities.
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

            // Define a workflow as data so we can store it somewhere (file, database, etc.).
            var workflowDefinition = new WorkflowDefinition
            {
                Activities = new[]
                {
                    new ActivityDefinition<WriteLine>("activity-1", new { TextExpression = new PlainTextExpression("Hello world!")}),
                    new ActivityDefinition<WriteLine>("activity-2", new { TextExpression = new PlainTextExpression("Goodbye cruel world...")})
                },
                Connections = new []
                {
                    new ConnectionDefinition("activity-1", "activity-2"), 
                }
            };
            
            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflowDefinition);

            Console.ReadLine();
        }
    }
}