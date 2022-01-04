using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class If : Activity
{
    [Input] public Input<bool> Condition { get; set; } = new(new Literal<bool>(false));
    [Outbound] public IActivity? Then { get; set; }
    [Outbound] public IActivity? Else { get; set; }
        
    protected override void Execute(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var nextNode = result ? Then : Else;

        if (nextNode != null)
            context.ScheduleActivity(nextNode, OnChildCompletedAsync);
    }

    private ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        return ValueTask.CompletedTask;
    }
}