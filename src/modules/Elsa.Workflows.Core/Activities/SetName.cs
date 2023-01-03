﻿using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Sets a property on the workflow execution context with the specified name value.
/// </summary>
[Activity("Elsa", "Workflows", "Set the name of the workflow instance to a specified value.")]
public class SetName : Activity
{
    /// <summary>
    /// The property key name used to store the workflow instance name.
    /// </summary>
    public const string WorkflowInstanceNameKey = "WorkflowInstanceName";

    /// <inheritdoc />
    [JsonConstructor]
    public SetName([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public SetName(Input<string> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
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
        var value = context.Get(Value);
        context.WorkflowExecutionContext.SetProperty(WorkflowInstanceNameKey, value!);
    }
}