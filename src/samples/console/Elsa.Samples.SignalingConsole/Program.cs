using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Models;
using Elsa.Activities.Signaling.Services;
using Elsa.Dispatch;
using Elsa.Models;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.SignalingConsole
{
    /// <summary>
    /// Demonstrates a workflow with a While looping construct.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddWorkflow<TrafficLightWorkflow>())
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Define a couple of cars so we can correlate workflows with them.
            var cars = new[] { "Car 1", "Car 2" };

            var workflowInstances = new List<WorkflowInstance>();

            // Execute a workflow for each car.
            foreach (var car in cars)
            {
                var workflowInstance = await workflowRunner.BuildAndStartWorkflowAsync<TrafficLightWorkflow>(correlationId: car);
                workflowInstances.Add(workflowInstance);
            }

            Console.WriteLine("Hit enter to signal green light for Car 2.");
            Console.ReadLine();

            // The workflows are now suspended at the red light.
            // Trigger a green light signal for the first car.
            var signal = "Green";
            var correlationId = "Car 2";
            var car2Workflow = workflowInstances.First(x => x.CorrelationId == correlationId);
            var signaler = services.GetRequiredService<ISignaler>();

            await signaler.TriggerSignalAsync(signal, workflowInstanceId: car2Workflow.Id);

            // Notice that only the workflow correlated to the second car executed.

            // Keep the application alive for the workflow scheduler to have enough time to resume the workflow. 
            Console.ReadLine();
        }
    }
}