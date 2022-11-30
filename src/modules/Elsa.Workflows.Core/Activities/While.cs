using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

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
        Behaviors.Add<BreakBehavior>(this);
        Behaviors.Remove<AutoCompleteBehavior>();
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

    [Input(AutoEvaluate = false)] public Input<bool> Condition { get; set; } = new(false);
    [Port] public IActivity Body { get; set; }

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
        var loop = await context.EvaluateInputPropertyAsync<While, bool>(x => x.Condition);

        if (loop)
            await context.ScheduleActivityAsync(Body, OnBodyCompleted);
        else
            await context.CompleteActivityAsync();
    }
}