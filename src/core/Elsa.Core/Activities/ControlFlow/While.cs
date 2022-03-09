using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class While : Activity
{
    public While()
    {
    }

    public While(Input<bool> condition)
    {
        Condition = condition;
    }

    public While(Func<ExpressionExecutionContext, ValueTask<bool>> condition) : this(new Input<bool>(condition))
    {
    }
    
    public While(Func<ExpressionExecutionContext, bool> condition) : this(new Input<bool>(condition))
    {
    }
    
    public While(Func<ValueTask<bool>> condition) : this(new Input<bool>(condition))
    {
    }
    
    public While(Func<bool> condition) : this(new Input<bool>(condition))
    {
    }

    [Input] public Input<bool> Condition { get; set; } = new(false);
    [Outbound] public IActivity Body { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var loop = context.Get(Condition);

        if (loop)
            context.SubmitActivity(Body, OnBodyCompleted);
    }

    private async ValueTask OnBodyCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var loop = await context.EvaluateAsync(Condition);

        if (loop)
            context.SubmitActivity(Body, OnBodyCompleted);
    }
}