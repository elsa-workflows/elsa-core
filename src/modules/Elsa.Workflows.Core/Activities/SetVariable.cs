using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Assign a workflow variable a value.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Primitives", "Assign a workflow variable a value.")]
public class SetVariable<T> : Activity
{
    /// <inheritdoc />
    public SetVariable([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public SetVariable(Variable<T> variable, Input<T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Variable = variable;
        Value = value;
    }

    /// <inheritdoc />
    public SetVariable(Variable<T> variable, Variable<T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(variable, new Input<T>(value), source, line)
    {
    }

    /// <inheritdoc />
    public SetVariable(Variable<T> variable, Func<ExpressionExecutionContext, T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(variable, new Input<T>(value), source, line)
    {
    }

    /// <inheritdoc />
    public SetVariable(Variable<T> variable, Func<T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(variable, new Input<T>(value), source, line)
    {
    }

    /// <inheritdoc />
    public SetVariable(Variable<T> variable, T value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(variable, new Input<T>(value), source, line)
    {
    }

    /// <summary>
    /// The variable to assign the value to.
    /// </summary>
    public Variable<T> Variable { get; set; } = default!;
    
    /// <summary>
    /// The value to assign.
    /// </summary>
    [Input] public Input<T> Value { get; set; } = new(new Literal<T>());

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var value = context.Get(Value);
        Variable.Set(context, value);
    }
}

/// <summary>
/// Assign a workflow variable a value.
/// </summary>
[Activity("Elsa", "Primitives", "Assign a workflow variable a value.")]
public class SetVariable : Activity
{
    /// <inheritdoc />
    public SetVariable([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The variable to assign the value to.
    /// </summary>
    [Input] public Variable Variable { get; set; } = default!;
    
    /// <summary>
    /// The value to assign.
    /// </summary>
    [Input] public Input<object?> Value { get; set; } = new(default(object));

    protected override void Execute(ActivityExecutionContext context)
    {
        var value = context.Get(Value);
        Variable.Set(context, value);
    }
}