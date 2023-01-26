using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// Loads and executes an <see cref="WorkflowDefinition"/>.
/// </summary>
public class WorkflowDefinitionActivity : Activity
{
    public string WorkflowDefinitionId { get; set; } = default!;
    
    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workflowDefinitionStore = context.GetRequiredService<IWorkflowDefinitionStore>();
        var workflowDefinition = await workflowDefinitionStore.FindPublishedByDefinitionIdAsync(WorkflowDefinitionId, context.CancellationToken);

        // Construct the root activity stored in the activity definitions.
        var materializer = context.GetRequiredService<IWorkflowMaterializer>();
        var root = await materializer.MaterializeAsync(workflowDefinition, context.CancellationToken);

        // Schedule the activity for execution.
        await context.ScheduleActivityAsync(root, OnChildCompletedAsync);
    }

    private async ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}