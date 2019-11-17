using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Reflection.Extensions;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample20.Workflows;

namespace Sample20
{
    /// <summary>
    /// Demonstrates Reflection activities
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddReflectionActivities()
                .AddConsoleActivities()
                .AddWorkflow<ExecuteMethodWorkflow>()
                .AddWorkflow<SplitObjectWorkflow>()
                .AddSingleton(Console.In)
                .BuildServiceProvider();

            // Invoke the workflows.
            var invoker = services.GetService<IWorkflowInvoker>();
            var executionContext1 = await invoker.StartAsync<ExecuteMethodWorkflow>();
            var executionContext2 = await invoker.StartAsync<SplitObjectWorkflow>();

            var serializer = services.GetRequiredService<IWorkflowSerializer>();
            
            // Test serialization.
            var json1 = serializer.Serialize(executionContext1.Workflow.ToInstance(), JsonTokenFormatter.FormatName);
            var json2 = serializer.Serialize(executionContext2.Workflow.ToInstance(), JsonTokenFormatter.FormatName);
            
            Console.WriteLine(json1);
            Console.WriteLine(json2);

            // Test deserialization.
            var workflowInstance1 = serializer.Deserialize<WorkflowInstance>(json1, JsonTokenFormatter.FormatName);
            var workflowInstance2 = serializer.Deserialize<WorkflowInstance>(json2, JsonTokenFormatter.FormatName);

            Console.ReadLine();
        }
    }
}