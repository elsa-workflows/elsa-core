using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

public enum ForOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}

[Activity("Elsa", "Control Flow", "Iterate over a sequence of steps between a start and an end number.")]
public class For : Activity
{
    private const string CurrentStepProperty = "CurrentStep";
    
    [JsonConstructor]
    public For()
    {
        Behaviors.Add<BreakBehavior>(this);
        Behaviors.Remove<AutoCompleteBehavior>();
    }

    public For(int start, int end, ForOperator forOperator = ForOperator.LessThanOrEqual) : this()
    {
        Start = new Input<int>(start);
        End = new Input<int>(end);
        Operator = new Input<ForOperator>(forOperator);
    }
    
    public Input<int> Start { get; set; } = new(0);
    public Input<int> End { get; set; } = new(0);
    public Input<int> Step { get; set; } = new(1);
    public Input<ForOperator> Operator { get; set; } = new(ForOperator.LessThanOrEqual);
    [Port] public IActivity? Body { get; set; }

    [JsonIgnore]
    [Output]
    public Output<MemoryBlockReference?> CurrentValue { get; set; } = new();

    protected override void Execute(ActivityExecutionContext context)
    {
        var iterateNode = Body;

        if (iterateNode == null)
            return;
        
        HandleIteration(context);
    }
        
    private void HandleIteration(ActivityExecutionContext context)
    {
        var iterateNode = Body!;
        var end = context.Get(End);
        var currentValue = context.GetProperty<int?>(CurrentStepProperty);

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

            // Update internal step.
            context.SetProperty(CurrentStepProperty, currentValue);
            
            // Update loop variable.
            context.Set(CurrentValue, currentValue);
            //CurrentValue?.Set(context.ExpressionExecutionContext, currentValue);
        }
    }

    private ValueTask OnChildComplete(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        HandleIteration(context);
        return ValueTask.CompletedTask;
    }
}