using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Activities.SetOutput;

/// <summary>
/// Assigns a given value to a workflow definition's output.
/// </summary>
[Activity("Elsa", "Composition", "Assigns a given value to a the container's output.", Kind = ActivityKind.Action)]
[PublicAPI]
public class SetOutput : CodeActivity
{
    /// <summary>
    /// The name of the output to assign.
    /// </summary>
    [Input(
        DisplayName = "Output",
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
    protected override void Execute(ActivityExecutionContext context)
    {
        var outputName = OutputName.Get(context);
        var ancestorContext = context.GetAncestors().FirstOrDefault(x => x.ActivityDescriptor.Outputs.Any(y => y.Name == outputName));

        if (ancestorContext != null)
            SetAncestorActivityOutput(context, ancestorContext);
    }

    private void SetAncestorActivityOutput(ActivityExecutionContext context, ActivityExecutionContext ancestorContext)
    {
        var ancestorActivity = ancestorContext.Activity;
        var ancestorActivityDescriptor = ancestorContext.ActivityDescriptor;
        var outputName = OutputName.Get(context);
        var ancestorOutputDescriptor = ancestorActivityDescriptor.Outputs.FirstOrDefault(x => x.Name == outputName);

        if (ancestorOutputDescriptor == null)
            return;

        var outputValue = OutputValue.GetOrDefault(context);
        var ancestorOutput = (Output)ancestorOutputDescriptor.ValueGetter(ancestorActivity)!;
        ancestorContext.Set(ancestorOutput, outputValue);

        // If the ancestor activity is the root workflow, we need to update the workflow execution context's output collection as well.
        if (ancestorContext.ParentActivityExecutionContext == null)
            context.WorkflowExecutionContext.Output[outputName] = outputValue!;
    }
}