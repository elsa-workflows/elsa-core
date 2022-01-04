using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Primitives;

public class UpdateVariable<T> : Activity
{
    [Input] public Input<T> Value { get; set; } = new Input<T>(new Literal<T>());
    public Variable<T> Variable { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var value = context.Get(Value);
        Variable.Set(context, value);
    }
}