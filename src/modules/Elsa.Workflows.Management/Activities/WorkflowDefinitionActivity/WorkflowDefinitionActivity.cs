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

        // Copy any collected outputs into the synthetic properties.
        foreach (var outputDescriptor in targetContext.ActivityDescriptor.Outputs)
        {
            // Create a local scope variable for each output property.
            var variable = new Variable
            {
                Id = outputDescriptor.Name,
                Name = outputDescriptor.Name
            };

            // Use the variable to read the value from the memory.
            var value = variable.Get(targetContext);

            // Assign the value to the output synthetic property.
            var output = SyntheticProperties.TryGetValue(outputDescriptor.Name, out var outputValue) ? (Output?)outputValue : default;
            targetContext.Set(output, value);
        }

        // Complete this activity with the signal value.
        await targetContext.CompleteActivityAsync(completeCompositeSignal?.Value);
    }

    private void CopyInputOutputToVariables(ActivityExecutionContext context)
    {
        var serviceProvider = context.GetRequiredService<IServiceProvider>();

        DeclareInputAsVariables(serviceProvider, (descriptor, variable) =>
        {
            var inputName = descriptor.Name;
            var input = SyntheticProperties.TryGetValue(inputName, out var inputValue) ? (Input?)inputValue : default;
            var evaluatedExpression = input != null ? context.Get(input.MemoryBlockReference()) : default;

            context.ExpressionExecutionContext.Memory.Declare(variable);
            variable.Set(context, evaluatedExpression);
        });

        DeclareOutputAsVariables(serviceProvider, (descriptor, variable) => context.ExpressionExecutionContext.Memory.Declare(variable));
    }

    private void DeclareInputAsVariables(IServiceProvider serviceProvider, Action<InputDescriptor, Variable> configureVariable)
    {
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(Type, Version)!;

        foreach (var inputDescriptor in activityDescriptor.Inputs)
        {
            var inputName = inputDescriptor.Name;
            var unsafeInputName = PropertyNameHelper.GetUnsafePropertyName(typeof(WorkflowDefinitionActivity), inputName);
            var variableType = typeof(Variable<>).MakeGenericType(inputDescriptor.Type);
            var variable = (Variable)Activator.CreateInstance(variableType)!;

            variable.Id = unsafeInputName;
            variable.Name = unsafeInputName;
            variable.StorageDriverType = inputDescriptor.StorageDriverType;

            configureVariable(inputDescriptor, variable);
        }
    }

    private void DeclareOutputAsVariables(IServiceProvider serviceProvider, Action<OutputDescriptor, Variable> configureVariable)
    {
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(Type, Version)!;

        foreach (var outputDescriptor in activityDescriptor.Outputs)
        {
            var outputName = outputDescriptor.Name;
            var unsafeOutputName = PropertyNameHelper.GetUnsafePropertyName(typeof(WorkflowDefinitionActivity), outputName);

            var variable = new Variable
            {
                Id = unsafeOutputName,
                Name = unsafeOutputName
            };

            configureVariable(outputDescriptor, variable);
        }
    }

    private async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var workflowDefinitionStore = serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId };

        if (!string.IsNullOrWhiteSpace(WorkflowDefinitionVersionId))
            filter.Id = WorkflowDefinitionVersionId;
        else
            filter.VersionOptions = VersionOptions.SpecificVersion(Version);

        var workflowDefinition =
            await workflowDefinitionStore.FindAsync(filter, cancellationToken)
            ?? (await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = VersionOptions.Published }, cancellationToken)
                ?? await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = VersionOptions.Latest }, cancellationToken));

        return workflowDefinition;
    }

    async ValueTask IInitializable.InitializeAsync(InitializationContext context)
    {
        var serviceProvider = context.ServiceProvider;
        var cancellationToken = context.CancellationToken;
        var workflowDefinition = await FindWorkflowDefinitionAsync(serviceProvider, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception($"Could not find workflow definition with ID {WorkflowDefinitionId}.");

        // Construct the root activity stored in the activity definitions.
        var materializer = serviceProvider.GetRequiredService<IWorkflowMaterializer>();
        var root = await materializer.MaterializeAsync(workflowDefinition, cancellationToken);

        // Declare input and output variables.
        DeclareInputAsVariables(serviceProvider, (inputDescriptor, variable) => Variables.Declare(variable));
        DeclareOutputAsVariables(serviceProvider, (outputDescriptor, variable) => Variables.Declare(variable));

        // Set the root activity.
        Root = root;
    }
}