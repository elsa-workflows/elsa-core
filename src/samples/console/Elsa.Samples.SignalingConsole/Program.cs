using System;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.Services;
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
            var cars = new[] {"Car 1", "Car 2"};

            // Execute a workflow for each car.
            foreach (var car in cars)
                await workflowRunner.BuildAndStartWorkflowAsync<TrafficLightWorkflow>(correlationId: car);

            Console.WriteLine("Hit enter to signal green light for Car 2.");
            Console.ReadLine();

            // The workflows are now suspended at the red light.
            // Trigger a green light signal for the first car.
            var signaler = services.GetRequiredService<ISignaler>();
            await signaler.TriggerSignalAsync("Green", workflowInstanceId: "Car 2");

            // Notice that only the workflow correlated to the second car executed.

            // Keep the application alive for the workflow scheduler to have enough time to resume the workflow. 
            Console.ReadLine();
        }
    }
}