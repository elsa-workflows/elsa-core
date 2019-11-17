using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Reflection.Extensions;
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
            await invoker.StartAsync<ExecuteMethodWorkflow>();
            await invoker.StartAsync<SplitObjectWorkflow>();

            Console.ReadLine();
        }
    }
}