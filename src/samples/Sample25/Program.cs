using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample25.Activities;

namespace Sample25
{
    /// <summary>
    /// A strongly-typed, long-running workflows program demonstrating data processing of sensor input from two channels.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddActivity<Absolute>()
                .AddActivity<Subtract>()
                .AddActivity<Channel>()
                .AddWorkflow<DataProcessingWorkflow>()
                .BuildServiceProvider();

            // Resolve services.
            var workflowInstanceStore = services.GetRequiredService<IWorkflowInstanceStore>();
            var invoker = services.GetService<IWorkflowInvoker>();
            
            // Create a new workflow instance that awaits sensor input.
            // The workflow will be suspended automatically because of the two blocking Channel activities.
            var workflowExecutionContext = await invoker.StartAsync<DataProcessingWorkflow>();
            
            // Get the workflow instance ID so we can resume it when we receive sensor data.
            var workflowInstanceId = workflowExecutionContext.Workflow.Id;

            // Simulate receival of sensor 1 data.
            Console.WriteLine("Simulate a value for Sensor 1:");
            var sensor1Value = double.Parse(Console.ReadLine()!);
            var sensor1Input = new Variables { ["Sensor1"] = new Variable(sensor1Value) };

            // Resume the workflow with received sensor data.
            var workflowInstance1 = await workflowInstanceStore.GetByIdAsync(workflowInstanceId);
            await invoker.ResumeAsync<DataProcessingWorkflow>(workflowInstance1, sensor1Input);

            // Simulate receival of sensor 2 data.
            Console.WriteLine("Simulate a value for Sensor 2:");
            var sensor2Value = double.Parse(Console.ReadLine()!);
            var sensor2Input = new Variables { ["Sensor2"] = new Variable(sensor2Value) };

            // Get the workflow instance to resume execution.
            var workflowInstance2 = await workflowInstanceStore.GetByIdAsync(workflowInstanceId);
            await invoker.ResumeAsync(workflowInstance2, sensor2Input);

            Console.ReadLine();
        }
    }
}