using System.ComponentModel;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Signals;
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
        var activityExecutionContext = context.TargetContext;

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
        foreach (var outputDescriptor in activityExecutionContext.ActivityDescriptor.Outputs)
        {
            var output = (Output?)outputDescriptor.ValueGetter(activityExecutionContext.Activity);
            // If direct output mapping is used, we can read the output value directly from the memory.
            var value = activityExecutionContext.Get(output) ?? activityExecutionContext.Get(outputDescriptor.Name);

            // Make sure to select a parent scope to avoid naming collisions between outputs defined on the current scope and outputs defined on parent scopes.
            var parentActivityExecutionContext = activityExecutionContext.ParentActivityExecutionContext?.GetAncestors()
                .Any(x => x.ActivityDescriptor.Outputs.Any(y => y.Name == outputDescriptor.Name)) == true
                ? activityExecutionContext.ParentActivityExecutionContext ?? activityExecutionContext
                : activityExecutionContext;
                
            parentActivityExecutionContext.Set(output, value, outputDescriptor.Name);
        }

        // Complete this activity with the signal value.
        await activityExecutionContext.CompleteActivityAsync(completeCompositeSignal?.Value);
    }

    private void CopyInputOutputToVariables(ActivityExecutionContext context)
    {
        var serviceProvider = context.GetRequiredService<IServiceProvider>();
        var activityDescriptor = FindActivityDescriptor(serviceProvider);

        DeclareInputAsVariables(activityDescriptor, (descriptor, variable) =>
        {
            var inputName = descriptor.Name;
            var input = SyntheticProperties.TryGetValue(inputName, out var inputValue) ? (Input?)inputValue : default;
            var evaluatedExpression = input != null ? context.Get(input.MemoryBlockReference()) : default;

            context.ExpressionExecutionContext.Memory.Declare(variable);
            variable.Set(context, evaluatedExpression);
        });

        DeclareOutputAsVariables(activityDescriptor, (descriptor, variable) => context.ExpressionExecutionContext.Memory.Declare(variable));
    }

    private void DeclareInputAsVariables(ActivityDescriptor activityDescriptor, Action<InputDescriptor, Variable> configureVariable)
    {
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

    private void DeclareOutputAsVariables(ActivityDescriptor activityDescriptor, Action<OutputDescriptor, Variable> configureVariable)
    {
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

    private async Task<Workflow?> FindWorkflowAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var workflowDefinitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var filter = new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId };

        if (!string.IsNullOrWhiteSpace(WorkflowDefinitionVersionId))
            filter.Id = WorkflowDefinitionVersionId;
        else
            filter.VersionOptions = VersionOptions.SpecificVersion(Version);

        var workflow =
            await workflowDefinitionService.FindWorkflowAsync(filter, cancellationToken)
            ?? (await workflowDefinitionService.FindWorkflowAsync(new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = VersionOptions.Published }, cancellationToken)
                ?? await workflowDefinitionService.FindWorkflowAsync(new WorkflowDefinitionFilter { DefinitionId = WorkflowDefinitionId, VersionOptions = VersionOptions.Latest }, cancellationToken));

        return workflow;
    }
    
    private ActivityDescriptor FindActivityDescriptor(IServiceProvider serviceProvider)
    {
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        return activityRegistry.Find(Type, Version) ?? activityRegistry.Find(Type) ?? throw new Exception($"Could not find activity descriptor for {Type}.");
    }

    async ValueTask IInitializable.InitializeAsync(InitializationContext context)
    {
        var serviceProvider = context.ServiceProvider;
        var cancellationToken = context.CancellationToken;
        var workflow = await FindWorkflowAsync(serviceProvider, cancellationToken);

        if (workflow == null)
            throw new Exception($"Could not find workflow definition with ID {WorkflowDefinitionId}.");

        var activityDescriptor = FindActivityDescriptor(serviceProvider);
        
        // Declare input and output variables.
        DeclareInputAsVariables(activityDescriptor, (_, variable) => Variables.Declare(variable));
        DeclareOutputAsVariables(activityDescriptor, (_, variable) => Variables.Declare(variable));

        // Set the root activity.
        Root = workflow;
    }
}