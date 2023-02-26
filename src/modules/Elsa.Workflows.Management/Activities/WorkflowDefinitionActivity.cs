using System.ComponentModel;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Activities;

/// <summary>
/// Loads and executes an <see cref="WorkflowDefinition"/>.
/// </summary>
[Browsable(false)]
public class WorkflowDefinitionActivity : Activity, IInitializable
{
    internal IActivity Root { get; set; } = default!;

    /// <summary>
    /// The definition ID of the workflow to schedule for execution.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = default!;

    /// <summary>
    /// the latest published version number set by the provider. This is used by tooling to let the user know that a newer version is available.
    /// </summary>
    public int LatestAvailablePublishedVersion { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Schedule the activity for execution.
        await context.ScheduleActivityAsync(Root, OnChildCompletedAsync);
    }

    private async ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var activityRegistry = context.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(Type, Version)!;
        var outputDescriptors = activityDescriptor.Outputs;

        foreach (var outputDescriptor in outputDescriptors)
        {
            var output = SyntheticProperties.TryGetValue(outputDescriptor.Name, out var outputProp) ? (Output)outputProp : default;

            if (output == null)
                return;

            // If there's a block with the same name as the output property, we need to read its value and bind it against our output.
            if (!context.ExpressionExecutionContext.Memory.HasBlock(outputDescriptor.Name))
                continue;

            var outputValue = context.ExpressionExecutionContext.Memory.Blocks[outputDescriptor.Name].Value;
            context.Set(output, outputValue);
        }

        await context.CompleteActivityAsync();
    }

    async ValueTask IInitializable.InitializeAsync(InitializationContext context)
    {
        var serviceProvider = context.ServiceProvider;
        var cancellationToken = context.CancellationToken;
        var workflowDefinitionStore = serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var versionOptions = VersionOptions.SpecificVersion(Version);
        var filter = new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = versionOptions };
        var workflowDefinition = await workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception($"Workflow definition {WorkflowDefinitionId} not found");

        // Construct the root activity stored in the activity definitions.
        var materializer = serviceProvider.GetRequiredService<IWorkflowMaterializer>();
        var root = await materializer.MaterializeAsync(workflowDefinition, cancellationToken);

        Root = root;
    }
}