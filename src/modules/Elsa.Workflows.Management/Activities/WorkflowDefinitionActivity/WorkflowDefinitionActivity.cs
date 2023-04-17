using System.ComponentModel;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;

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
    /// The latest published version number set by the provider. This is used by tooling to let the user know that a newer version is available.
    /// </summary>
    public int LatestAvailablePublishedVersion { get; set; }

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
                Name = outputDescriptor.Name
            };

            Variables.Declare(variable);
        }
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
        }

        // Do we have a complete composite signal that triggered the completion?
        var completeCompositeSignal = context.WorkflowExecutionContext.TransientProperties.TryGetValue(nameof(CompleteCompositeSignal), out var signal) ? (CompleteCompositeSignal)signal : default;

        await context.CompleteActivityAsync(completeCompositeSignal?.Value);
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

        DeclareInputOutputAsVariables(context);
        
        Root = root;
    }
}