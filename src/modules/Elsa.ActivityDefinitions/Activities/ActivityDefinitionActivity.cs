using System.Text.Json;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;

namespace Elsa.ActivityDefinitions.Activities;

/// <summary>
/// A wrapper for custom activity definitions
/// </summary>
public class ActivityDefinitionActivity : ActivityBase
{
    public string DefinitionId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var store = context.GetRequiredService<IActivityDefinitionStore>();
        var definition = await store.FindByDefinitionIdAsync(DefinitionId, VersionOptions.Published, context.CancellationToken);

        if (definition == null)
        {
            await CompleteAsync(context);
            return;
        }

        var serializerOptionsProvider = context.GetRequiredService<SerializerOptionsProvider>();
        var root = JsonSerializer.Deserialize<IActivity>(definition.Data!, serializerOptionsProvider.CreateDefaultOptions())!;
        context.ScheduleActivity(root, onChildCompletedAsync);
    }
    
    private async ValueTask onChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}