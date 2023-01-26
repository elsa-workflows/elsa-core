using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// Loads & executes an <see cref="WorkflowDefinition"/>.
/// </summary>
public class WorkflowDefinitionActivity : Activity
{
    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Construct the root activity stored in the activity definitions.
        var materializer = context.GetRequiredService<IWorkflowDefinitionActivityMaterializer>();
        var root = await materializer.MaterializeAsync(this, context.CancellationToken);

        // Schedule the activity for execution.
        await context.ScheduleActivityAsync(root, OnChildCompletedAsync);
    }

    private async ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}