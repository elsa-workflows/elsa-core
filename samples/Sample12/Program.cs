using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Primitives;
using Elsa.Activities.UserTask.Activities;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.Memory;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Sample12
{
    /// <summary>
    /// Demonstrates workflow correlation.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var services = BuildServices();
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = registry.RegisterWorkflow<UserTaskWorkflow>();
            var invoker = services.GetRequiredService<IWorkflowInvoker>();

            // Invoke the workflow.
            var correlationId = Guid.NewGuid().ToString("N");
            await invoker.InvokeAsync(workflowDefinition, correlationId: correlationId);
            WorkflowExecutionContext executionContext;
            
            do
            {
                // Workflow is now halted on the user task activity. Ask user for input:
                Console.WriteLine("What action will you take? Choose one of: Accept, Reject, Needs Work");
                var userAction = Console.ReadLine();

                // Resume the workflow with the received stimulus.
                var triggeredExecutionContexts = await invoker.TriggerAsync(nameof(UserTask), new Variables { ["UserAction"] = userAction}, correlationId);
                executionContext = triggeredExecutionContexts.First();

            } while (executionContext.Workflow.IsHalted());
        }

        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddWorkflows()
                .AddConsoleActivities()
                .AddUserTaskActivities()
                .AddMemoryWorkflowDefinitionStore()
                .AddMemoryWorkflowInstanceStore()
                .BuildServiceProvider();
        }
    }
}