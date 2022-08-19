using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.BuildAndDispatchConsole
{
    /// <summary>
    /// Demonstrates a workflow with a For looping construct.
    /// </summary>
    static class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddWorkflow<DemoWorkflow>())
                .BuildServiceProvider();
            
            // Get a workflow builder. This is a transient service.
            var workflowBuilder = services.GetRequiredService<IWorkflowBuilder>();
            
            // Build a workflow.
            var workflowBlueprint = workflowBuilder.Build<DemoWorkflow>();
            
            // Create a workflow instance.
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var workflowInstance = await workflowFactory.InstantiateAsync(workflowBlueprint);

            // Persist the workflow instance before dispatching.
            var workflowInstanceStore = services.GetRequiredService<IWorkflowInstanceStore>();
            await workflowInstanceStore.SaveAsync(workflowInstance);
            
            // Get workflow dispatcher.
            var workflowDispatcher = services.GetRequiredService<IWorkflowInstanceDispatcher>();

            // Dispatch the workflow for execution.
            await workflowDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(workflowInstance.Id));
            
            // Keep console alive.
            Console.ReadLine();
        }
    }
}