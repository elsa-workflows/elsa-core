using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Activities.SetWorkflowOutput;

/// <summary>
/// Assigns a given value to a workflow definition's output.
/// </summary>
[Activity("Elsa", "Composition", "Assigns a given value to a workflow definition's output.", Kind = ActivityKind.Action)]
[PublicAPI]
public class SetWorkflowOutput : CodeActivity
{
    /// <summary>
    /// The name of the output to assign.
    /// </summary>
    [Input(
        Description = "The output to assign.",
        UIHint = InputUIHints.OutputPicker
    )]
    public Input<string> OutputName { get; set; } = default!;

    /// <summary>
    /// The value to assign to the output.
    /// </summary>
    [Input(Description = "The value to assign.")]
    public Input<object?> OutputValue { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Get the first workflow definition activity in scope, so that we can get its output definitions.
        var workflowDefinitionActivityContext = context.GetFirstWorkflowDefinitionActivityExecutionContext();

        if (workflowDefinitionActivityContext != null)
            // Set the output on the first workflow definition activity execution context in scope.
            await SetWorkflowDefinitionActivityOutputAsync(context, workflowDefinitionActivityContext);
        else
            // If no workflow definition activity is in scope, set the output on the workflow execution context.
            SetWorkflowDefinitionOutput(context);
    }

    private void SetWorkflowDefinitionOutput(ActivityExecutionContext context)
    {
        var outputName = OutputName.Get(context);
        var workflowActivityContext = context.GetAncestors().Last(x => x.Activity is Workflow);
        var outputValue = OutputValue.Get(context);
        workflowActivityContext.WorkflowExecutionContext.Output[outputName] = outputValue!;
    }

    private async Task SetWorkflowDefinitionActivityOutputAsync(ActivityExecutionContext context, ActivityExecutionContext workflowDefinitionActivityContext)
    {
        var workflowDefinitionStore = context.GetRequiredService<IWorkflowDefinitionStore>();
        var workflowDefinitionActivity = (WorkflowDefinitionActivity.WorkflowDefinitionActivity)workflowDefinitionActivityContext.Activity;
        var definitionId = workflowDefinitionActivity.WorkflowDefinitionId;
        var versionOptions = VersionOptions.SpecificVersion(workflowDefinitionActivity.Version);
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = versionOptions};
        var workflowDefinition = await workflowDefinitionStore.FindAsync(filter, context.CancellationToken);

        if (workflowDefinition == null)
            return;

        var outputName = OutputName.Get(context);
        var outputDescriptor = workflowDefinition.Outputs.FirstOrDefault(x => x.Name == outputName);

        if (outputDescriptor == null)
            return;

        var output = workflowDefinitionActivity.SyntheticProperties.TryGetValue(outputDescriptor.Name, out var outputProp) ? (Output)outputProp : default;

        if (output == null)
            return;

        var outputValue = OutputValue.Get(context);
        context.Set(output, outputValue);
    }
}