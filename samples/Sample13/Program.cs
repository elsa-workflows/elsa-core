using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.Memory;
using Elsa.Scripting;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample13
{
    /// <summary>
    /// A strongly-typed workflows program demonstrating scripting, and branching.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflows()
                .AddMemoryWorkflowInstanceStore()
                .AddConsoleActivities()
                .AddSingleton<IScriptEngineConfigurator, ScriptEngineConfigurator>()
                .BuildServiceProvider();

            // Create a workflow.
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var workflow = workflowFactory.CreateWorkflow<ActivityOutputWorkflow>();

            // Start the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.StartAsync(workflow);

            Console.ReadLine();
        }
    }
}