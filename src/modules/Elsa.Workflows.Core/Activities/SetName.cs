using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Sets a transient property on the workflow execution context the specified name value.
/// This value is used by the <see cref="PersistWorkflowInstanceMiddleware"/> component to update the name of the workflow instance. 
/// </summary>
[Activity("Elsa", "Workflows", "Set the name of the workflow instance to a specified value.")]
public class SetName : Activity
{
    internal static readonly object WorkflowInstanceNameKey = new();

    [JsonConstructor]
    public SetName([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    public SetName(Input<string> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Value = value;
    }
    
    /// <summary>
    /// The value to set the workflow instance's name to.
    /// </summary>
    public Input<string> Value { get; set; } = new("");

    protected override void Execute(ActivityExecutionContext context)
    {
        var value = context.Get(Value);
        context.WorkflowExecutionContext.TransientProperties[WorkflowInstanceNameKey] = value!;
    }
}