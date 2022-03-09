using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class While : Activity
{
    public While(IActivity? body = default)
    {
        Body = body!;
    }

    public While(Input<bool> condition, IActivity? body = default) : this(body)
    {
        Condition = condition;
    }

    public While(Func<ExpressionExecutionContext, ValueTask<bool>> condition, IActivity? body = default) : this(new Input<bool>(condition), body)
    {
    }
    
    public While(Func<ExpressionExecutionContext, bool> condition, IActivity? body = default) : this(new Input<bool>(condition), body)
    {
    }
    
    public While(Func<ValueTask<bool>> condition, IActivity? body = default) : this(new Input<bool>(condition), body)
    {
    }
    
    public While(Func<bool> condition, IActivity? body = default) : this(new Input<bool>(condition), body)
    {
    }

    [Input] public Input<bool> Condition { get; set; } = new(false);
    [Outbound] public IActivity Body { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var loop = context.Get(Condition);

        if (loop)
            context.PostActivity(Body, OnBodyCompleted);
    }

    private async ValueTask OnBodyCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var loop = await context.EvaluateAsync(Condition);

        if (loop)
            context.PostActivity(Body, OnBodyCompleted);
    }
}