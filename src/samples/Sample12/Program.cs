using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.UserTask.Activities;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Sample12
{
    /// <summary>
    /// Demonstrates workflow correlation & user tasks.
    /// </summary>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var services = BuildServices();
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = await registry.GetWorkflowDefinitionAsync<UserTaskWorkflow>();
            var invoker = services.GetRequiredService<IWorkflowRunner>();

            // Start the workflow.
            var correlationId = Guid.NewGuid().ToString("N");
            await invoker.RunAsync(workflowDefinition, correlationId: correlationId);
            WorkflowExecutionContext executionContext;
            
            do
            {
                // Workflow is now halted on the user task activity. Ask user for input:
                Console.WriteLine("What action will you take? Choose one of: Accept, Reject, Needs Work");
                var userAction = Console.ReadLine();

                // Resume the workflow with the received stimulus.
                var triggeredExecutionContexts = await invoker.TriggerAsync(nameof(UserTask), new Variable(userAction), correlationId);
                executionContext = triggeredExecutionContexts.First();

            } while (executionContext.Workflow.IsRunning());
        }

        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddWorkflow<UserTaskWorkflow>()
                .BuildServiceProvider();
        }
    }
}