using System.Threading.Tasks;

using Elsa.Activities.Entity;
using Elsa.Activities.Entity.Triggers;
using Elsa.Activities.Entity.Extensions;
using Elsa.Services;
using Elsa.Triggers;

using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Elsa.Samples.EntityChanged
{
    /// <summary>
    /// Demonstrates the use of declarative composite activities.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddEntityActivities()
                .AddWorkflow<EntityChangedWorkflow>()
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            var workflowSelector = services.GetRequiredService<IWorkflowSelector>();

            var workflowSelectionResult = (await workflowSelector.SelectWorkflowsAsync<EntityChangedTrigger>(evalute => 
                (evalute.Action.HasValue == false || evalute.Action == EntityChangedAction.Added)
                && (evalute.EntityName == null || evalute.EntityName == typeof(Entity).GetEntityName()))).First();

            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

            await workflowRunner.RunWorkflowAsync(workflowSelectionResult.WorkflowBlueprint, workflowSelectionResult.ActivityId);
          
        }

    }
}