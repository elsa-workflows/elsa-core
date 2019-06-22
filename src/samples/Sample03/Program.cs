using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Serialization.Models;
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
                    new Connection("activity-1", "activity-2")
                }
            };
            
            // Create a workflow from the definition.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            var workflow = workflowBuilder.Build(workflowDefinition);

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflow);

            Console.ReadLine();
        }
    }
}