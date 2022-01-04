using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public enum ForOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}

public class For : Activity
{
    public Input<int> Start { get; set; } = new(0);
    public Input<int> End { get; set; } = new(0);
    public Input<int> Step { get; set; } = new(1);
    public Input<ForOperator> Operator { get; set; } = new(ForOperator.LessThanOrEqual);
    [Outbound] public IActivity? Body { get; set; }
    public Variable<int?> CurrentValue { get; set; } = new();

    protected override void Execute(ActivityExecutionContext context)
    {
        var iterateNode = Body;

        if (iterateNode == null)
            return;

        context.ExpressionExecutionContext.Register.Declare(CurrentValue);
        HandleIteration(context);
    }
        
    private void HandleIteration(ActivityExecutionContext context)
    {
        var iterateNode = Body!;
        var end = context.Get(End);
        var currentValue = CurrentValue.Get<int?>(context.ExpressionExecutionContext);

        // Initialize or increment.
        var start = context.Get(Start);
        var step = context.Get(Step);
        var op = context.Get(Operator); 
        currentValue = currentValue == null ? start : currentValue + step;

        var loop = op switch
        {
            ForOperator.LessThan => currentValue < end,
            ForOperator.LessThanOrEqual => currentValue <= end,
            ForOperator.GreaterThan => currentValue > end,
            ForOperator.GreaterThanOrEqual => currentValue >= end,
            _ => throw new NotSupportedException()
        };

        if (loop)
        {
            context.ScheduleActivity(iterateNode, OnChildComplete);

            // Update loop variable.
            CurrentValue.Set(context.ExpressionExecutionContext, currentValue);
        }
    }

    private ValueTask OnChildComplete(ActivityExecutionContext completedActivityExecutionContext, ActivityExecutionContext ownerActivityExecutionContext)
    {
        HandleIteration(ownerActivityExecutionContext);
        return ValueTask.CompletedTask;
    }
}