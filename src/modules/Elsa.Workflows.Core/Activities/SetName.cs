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
    public SetName()
    {
    }

    public SetName(Input<string> value)
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