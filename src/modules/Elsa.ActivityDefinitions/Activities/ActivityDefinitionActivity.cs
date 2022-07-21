using System.Text.Json;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
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
        var store = context.GetRequiredService<IActivityDefinitionStore>();
        var definition = await store.FindByDefinitionIdAsync(DefinitionId, VersionOptions.SpecificVersion(DefinitionVersion), context.CancellationToken);

        if (!await CompleteIfNotFoundAsync(context, definition))
            return;

        // Construct the root activity stored in the activity definitions.
        var materializer = context.GetRequiredService<IActivityDefinitionMaterializer>();
        var root = await materializer.MaterializeAsync(definition!, context.CancellationToken);

        // Schedule the activity for execution.
        context.ScheduleActivity(root, onChildCompletedAsync);
    }

    private async Task<bool> CompleteIfNotFoundAsync(ActivityExecutionContext context, ActivityDefinition? definition)
    {
        if (definition != null)
            return true;

        // If no definition found, then there's nothing to execute and we can complete.
        var logger = context.GetRequiredService<ILogger<ActivityDefinitionActivity>>();
        logger.LogWarning(
            "Activity {ActivityId} is configured to execute activity definition {ActivityDefinitionId} with version {ActivityDefinitionVersion}, but no such version was found. No activity definition will be executed",
            Id,
            DefinitionId,
            DefinitionVersion);

        await CompleteAsync(context);
        return false;
    }

    private async ValueTask onChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}