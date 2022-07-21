using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.ActivityDefinitions.Activities;

/// <summary>
/// Loads & executes an <see cref="ActivityDefinition"/>.
/// </summary>
public class ActivityDefinitionActivity : ActivityBase
{
    /// <summary>
    /// The activity definition ID to load & execute.
    /// </summary>
    public string DefinitionId { get; set; } = default!;

    /// <summary>
    /// The activity definition version number to load & execute.
    /// </summary>
    public int DefinitionVersion { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Construct the root activity stored in the activity definitions.
        var materializer = context.GetRequiredService<IActivityDefinitionMaterializer>();
        var root = await materializer.MaterializeAsync(this, context.CancellationToken);

        // Schedule the activity for execution.
        context.ScheduleActivity(root, onChildCompletedAsync);
    }

    private async ValueTask onChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}