using System;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.Extensions;
using Elsa.Multitenancy;
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
            var serviceCollection = new ServiceCollection().AddElsaServices();

            var services = MultitenantContainerFactory.CreateSampleMultitenantContainer(serviceCollection,
                options => options
                    .AddConsoleActivities()
                    .AddWorkflowsFrom<SignalReceiverWorkflow>());
            
            Console.WriteLine("Press enter to send a signal from a workflow:");
            Console.ReadLine();
            
            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            
            // This workflow will send a signal, which will be handled by the `SignalReceiverWorkflow`.
            await workflowRunner.BuildAndStartWorkflowAsync<SignalSenderWorkflow>();
            
            // We can also trigger a signal manually:
            Console.WriteLine();
            Console.WriteLine("Press enter again to send a signal manually:");
            Console.ReadLine();
            
            var signaler = services.GetRequiredService<ISignaler>();
            await signaler.TriggerSignalAsync("Demo Signal");

            // Keep the application alive for the workflow scheduler to have enough time to resume the workflow. 
            Console.ReadLine();
        }
    }
}