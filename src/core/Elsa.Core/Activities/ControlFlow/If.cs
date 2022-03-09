using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class If : Activity
{
    public If()
    {
    }

    public If(Input<bool> condition) => Condition = condition;
    public If(Func<ExpressionExecutionContext, bool> condition) => Condition = new Input<bool>(condition);
    public If(Func<bool> condition) => Condition = new Input<bool>(condition);

    [Input] public Input<bool> Condition { get; set; } = new(new Literal<bool>(false));
    [Outbound] public IActivity? Then { get; set; }
    [Outbound] public IActivity? Else { get; set; }
        
    protected override void Execute(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var nextNode = result ? Then : Else;

        if (nextNode != null)
            context.PostActivity(nextNode, OnChildCompletedAsync);
    }

    private ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        return ValueTask.CompletedTask;
    }
}