using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;

namespace Elsa.ActivityDefinitions.Activities;

/// <summary>
/// Loads & executes an <see cref="ActivityDefinition"/>.
/// </summary>
public class ActivityDefinitionActivity : Activity
{
    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Construct the root activity stored in the activity definitions.
        var materializer = context.GetRequiredService<IActivityDefinitionMaterializer>();
        var root = await materializer.MaterializeAsync(this, context.CancellationToken);

        // Schedule the activity for execution.
        await context.ScheduleActivityAsync(root, OnChildCompletedAsync);
    }

    private async ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}