using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class While : Activity
{
    [Input] public Input<bool> Condition { get; set; } = new(false);
    [Outbound] public IActivity Body { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var loop = context.Get(Condition);
            
        if(loop)
            context.ScheduleActivity(Body, OnBodyCompleted);
    }

    private async ValueTask OnBodyCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var loop = await context.EvaluateAsync(Condition);
            
        if(loop)
            context.ScheduleActivity(Body, OnBodyCompleted);
    }
}