using System.ComponentModel;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Loads and executes an <see cref="WorkflowDefinition"/>.
/// </summary>
[Browsable(false)]
public class WorkflowDefinitionActivity : Composite, IInitializable
{
    /// <summary>
    /// The definition ID of the workflow to schedule for execution.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = default!;

    /// <summary>
    /// The specific version ID of the workflow to schedule for execution. If not set, the <see cref="Version"/> number will be considered.
    /// </summary>
    public string? WorkflowDefinitionVersionId { get; set; }

    /// <summary>
    /// The latest published version number set by the provider. This is used by tooling to let the user know that a newer version is available.
    /// </summary>
    public int LatestAvailablePublishedVersion { get; set; }

    /// <summary>
    /// The latest published version ID set by the provider. This is used by tooling to let the user know that a newer version is available.
    /// </summary>
    public string? LatestAvailablePublishedVersionId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        CopyInputOutputToVariables(context);
        await context.ScheduleActivityAsync(Root, OnChildCompletedAsync);
    }

    private void CopyInputOutputToVariables(ActivityExecutionContext context)
    {
        foreach (var inputDescriptor in context.ActivityDescriptor.Inputs)
        {
            var input = SyntheticProperties.TryGetValue(inputDescriptor.Name, out var inputValue) ? (Input?)inputValue : default;
            var evaluatedExpression = input != null ? context.Get(input.MemoryBlockReference()) : default;

            // Create a local scope variable for each input property.
            var variable = new Variable
            {
                Id = inputDescriptor.Name,
                Name = inputDescriptor.Name,
                StorageDriverType = inputDescriptor.StorageDriverType
            };

            context.ExpressionExecutionContext.Memory.Declare(variable);
            variable.Set(context, evaluatedExpression);
        }

        foreach (var outputDescriptor in context.ActivityDescriptor.Outputs)
        {
            // Create a local scope variable for each output property.
            var variable = new Variable
            {
                Id = outputDescriptor.Name,
                Name = outputDescriptor.Name
            };

            context.ExpressionExecutionContext.Memory.Declare(variable);
        }
    }

    private void DeclareInputOutputAsVariables(InitializationContext context)
    {
        var activityRegistry = context.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(Type, Version)!;

        // Declare input variables.
        foreach (var inputDescriptor in activityDescriptor.Inputs)
        {
            // Create a local scope variable for each input property.
            var variable = new Variable(inputDescriptor.Name)
            {
                Id = inputDescriptor.Name,
                Name = inputDescriptor.Name,
                StorageDriverType = inputDescriptor.StorageDriverType
            };

            Variables.Declare(variable);
        }

        // Declare output variables.
        foreach (var outputDescriptor in activityDescriptor.Outputs)
        {
            // Create a local scope variable for each output property.
            var variable = new Variable(outputDescriptor.Name)
            {
                Id = outputDescriptor.Name,
                Name = outputDescriptor.Name
            };

            Variables.Declare(variable);
        }
    }

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;

        // Do we have a "complete composite" signal that triggered the completion?
        var completeCompositeSignal = context.WorkflowExecutionContext.TransientProperties.TryGetValue(nameof(CompleteCompositeSignal), out var signal) ? (CompleteCompositeSignal)signal : default;

        // If we do, make sure to remove it from the transient properties.
        if (completeCompositeSignal != null)
        {
            var logger = context.GetRequiredService<ILogger<WorkflowDefinitionActivity>>();
            logger.LogDebug("Received a complete composite signal and removing it from the transient properties");
            context.WorkflowExecutionContext.TransientProperties.Remove(nameof(CompleteCompositeSignal));
        }

        await targetContext.CompleteActivityAsync(completeCompositeSignal?.Value);
    }

    async ValueTask IInitializable.InitializeAsync(InitializationContext context)
    {
        var serviceProvider = context.ServiceProvider;
        var cancellationToken = context.CancellationToken;
        var workflowDefinitionStore = serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId };

        if (!string.IsNullOrWhiteSpace(WorkflowDefinitionVersionId))
            filter.Id = WorkflowDefinitionVersionId;
        else
            filter.VersionOptions = VersionOptions.SpecificVersion(Version);

        var workflowDefinition = await workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (workflowDefinition == null)
        {
            // Find the latest published version.
            workflowDefinition = await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = VersionOptions.Published }, cancellationToken);

            if (workflowDefinition == null)
            {
                // Find the latest version.
                workflowDefinition = await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = VersionOptions.Latest }, cancellationToken);
            }

            if (workflowDefinition == null)
                throw new Exception($"Could not find workflow definition with ID {WorkflowDefinitionId}.");
        }

        // Construct the root activity stored in the activity definitions.
        var materializer = serviceProvider.GetRequiredService<IWorkflowMaterializer>();
        var root = await materializer.MaterializeAsync(workflowDefinition, cancellationToken);

        DeclareInputOutputAsVariables(context);

        Root = root;
    }
}