using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Workflows.Activities;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample11
{
    /// <summary>
    /// Demonstrates workflow correlation.
    /// </summary>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var services = BuildServices();
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = await registry.GetWorkflowDefinitionAsync<CorrelationWorkflow>();
            var invoker = services.GetRequiredService<IWorkflowInvoker>();

            Console.WriteLine("How many workflow instances should be started? Enter a number:");
            var instanceCount = int.Parse(Console.ReadLine());
            
            for (var i = 0; i < instanceCount; i++)
                await invoker.StartAsync(workflowDefinition, correlationId: $"document {i + 1}");

            var retry = true;

            while (retry)
            {
                Console.WriteLine();
                Console.WriteLine("Now, enter the correlation ID of the workflow to resume:");
                var correlationId = Console.ReadLine();

                // Resume one workflow using the specified correlation ID.
                var triggeredExecutionContexts = (await invoker.TriggerAsync(nameof(Signaled), new Variables() { ["Signal"] = new Variable("Proceed") }, correlationId)).ToList();

                Console.WriteLine("{0} workflow was resumed. Would you like to trigger another?", triggeredExecutionContexts.Count);
                retry = string.Equals("y", Console.ReadLine(), StringComparison.OrdinalIgnoreCase);
            }

            Console.WriteLine("Bye!");
        }

        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddWorkflow<CorrelationWorkflow>()
                .BuildServiceProvider();
        }
    }
}