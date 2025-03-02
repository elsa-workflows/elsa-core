using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Sets a property on the workflow execution context with the specified name value.
/// </summary>
[Activity("Elsa", "Primitives", "Set the name of the workflow instance to a specified value.")]
[PublicAPI]
public class SetName : CodeActivity
{
    /// <inheritdoc />
    public SetName([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public SetName(Input<string> value, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        Value = value;
    }
    
    /// <summary>
    /// The value to set the workflow instance's name to.
    /// </summary>
    public Input<string> Value { get; set; } = new("");

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var value = Value.GetOrDefault(context);
        context.WorkflowExecutionContext.Name = value;
    }
}