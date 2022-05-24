using System.ComponentModel;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities;

[Browsable(false)]
[Activity("Elsa", "Primitives", "Set a workflow variable to a given value.")]
public class SetVariable<T> : Activity
{
    public SetVariable()
    {
    }

    public SetVariable(Variable<T> variable, Input<T> value)
    {
        Variable = variable;
        Value = value;
    }
    
    public SetVariable(Variable<T> variable, Func<ExpressionExecutionContext, T> value) : this(variable, new Input<T>(value))
    {
    }
    
    public SetVariable(Variable<T> variable, Func<T> value) : this(variable, new Input<T>(value))
    {
    }
    
    public SetVariable(Variable<T> variable, T value) : this(variable, new Input<T>(value))
    {
    }

    [Input] public Input<T> Value { get; set; } = new(new Literal<T>());
    public Variable<T> Variable { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var value = context.Get(Value);
        Variable.Set(context, value);
    }
}

[Activity("Elsa", "Primitives", "Set a workflow variable to a given value.")]
public class SetVariable : Activity
{
    [Input] public Input<object?> Value { get; set; } = new(default(object));
    public Variable Variable { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var value = context.Get(Value);
        Variable.Set(context, value);
    }
}