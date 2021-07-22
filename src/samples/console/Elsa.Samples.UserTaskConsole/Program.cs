using Elsa.Activities.UserTask.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Samples.UserTaskConsole
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddUserTaskActivities()
                    .AddWorkflow<UserTaskWorkflow>())
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // And an interruptor.
            var workflowTriggerInterruptor = services.GetRequiredService<IWorkflowTriggerInterruptor>();

            // Execute the workflow.
            var runWorkflowResult = await workflowRunner.BuildAndStartWorkflowAsync<UserTaskWorkflow>();
            var workflowInstance = runWorkflowResult.WorkflowInstance!;

            var availableActions = new[]
            {
                "Accept",
                "Reject",
                "Needs Work"
            };

            // Workflow is now halted on the user task activity. Ask user for input:
            Console.WriteLine($"What action will you take? Choose one of: {string.Join(", ", availableActions)}");
            var userAction = Console.ReadLine();
            var currentActivityId = workflowInstance.BlockingActivities.Select(i => i.ActivityId).First();
            await workflowTriggerInterruptor.InterruptActivityAsync(workflowInstance, currentActivityId, new WorkflowInput(userAction));
        }
    }
}