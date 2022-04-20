using System.Text.Json.Serialization;
using Elsa.Attributes;
using Elsa.Behaviors;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities;

[Activity("Elsa", "Primitives", "Execute an activity while a given condition evaluates to true.")]
public class While : Activity
{
    public static While True(IActivity body) => new(body)
    {
        Condition = new Input<bool>(true)
    };
    
    [JsonConstructor]
    public While(IActivity? body = default)
    {
        Body = body!;
        Behaviors.Add<BreakBehavior>();
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
    [Outbound] public IActivity Body { get; set; }

    protected override bool CompleteImplicitly => false;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await HandleIterationAsync(context);
    }

    private async ValueTask OnBodyCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await HandleIterationAsync(context);
    }

    private async ValueTask HandleIterationAsync(ActivityExecutionContext context)
    {
        var loop = context.Get(Condition);

        if (loop)
            context.ScheduleActivity(Body, OnBodyCompleted);
        else
            await context.CompleteActivityAsync();
    }
}