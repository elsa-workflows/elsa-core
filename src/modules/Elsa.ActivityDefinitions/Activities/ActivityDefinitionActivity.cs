using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.ActivityDefinitions.Activities;

/// <summary>
/// Loads & executes an <see cref="ActivityDefinition"/>.
/// </summary>
public class ActivityDefinitionActivity : ActivityBase
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Construct the root activity stored in the activity definitions.
        var materializer = context.GetRequiredService<IActivityDefinitionMaterializer>();
        var root = await materializer.MaterializeAsync(this, context.CancellationToken);

        // Schedule the activity for execution.
        await context.ScheduleActivityAsync(root, onChildCompletedAsync);
    }

    private async ValueTask onChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}