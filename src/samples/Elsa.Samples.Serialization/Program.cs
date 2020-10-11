using System;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Serialization
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();
            
            // Build a new workflow.
            var workflow = services.GetService<IWorkflowBuilder>()
                //.Then<WriteLine>(writeLine => writeLine.)
                .WriteLine("Hello World!")
                .Build();

            // Get a reference to the workflow serializer. 
            var serializer = services.GetRequiredService<IWorkflowSerializer>();
            
            // Serialize the workflow.
            var json = serializer.Serialize(workflow, SerializationFormats.Json);
            Console.WriteLine(json);
            
            // Deserialize the workflow.
            var deserializedWorkflow = serializer.Deserialize<WorkflowBlueprint>(json, SerializationFormats.Json);
            
            // Get the workflow host.
            var workflowHost = services.GetService<IWorkflowHost>();
            
            // Execute the workflow.
            await workflowHost.RunWorkflowAsync(deserializedWorkflow);
            
        }
    }
}