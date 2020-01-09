using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample04
{
    /// <summary>
    /// A workflow built using a fluent API and then executed using a scheduler.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .BuildServiceProvider();

            // Get a workflow builder.
            var workflowBuilder = services.GetRequiredService<WorkflowBuilder>();
            var workflow = workflowBuilder.Build<HelloWorldWorkflow>();
            
            // Get a scheduler.
            var scheduler = services.GetRequiredService<IScheduler>();

            // Run the workflow.
            //await scheduler.ScheduleActivityAsync(workflow);
        }
    }
}