using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Control Flow", "Iterate over a sequence of steps between a start and an end number.")]
public class For : ActivityBase
{
    private const string CurrentStepProperty = "CurrentStep";

    [JsonConstructor]
    public For()
    {
        Behaviors.Add<BreakBehavior>(this);
    }

    public For(int start, int end) : this()
    {
        Start = new Input<int>(start);
        End = new Input<int>(end);
    }

    public Input<int> Start { get; set; } = new(0);
    public Input<int> End { get; set; } = new(0);
    public Input<int> Step { get; set; } = new(1);

    /// <summary>
    /// Controls whether the end step is upper/lowerbound inclusive or exclusive. True (inclusive) by default.
    /// </summary>
    public Input<bool> OuterBoundInclusive { get; set; } = new(true);

    /// <summary>
    /// The activity to execute for each iteration.
    /// </summary>
    [Port]
    public IActivity? Body { get; set; }

    /// <summary>
    /// Stores the current value for each iteration. 
    /// </summary>
    [JsonIgnore]
    [Output]
    public Output<MemoryBlockReference?> CurrentValue { get; set; } = new();

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var iterateNode = Body;

        if (iterateNode == null)
            return;

        await HandleIteration(context);
    }

    private async ValueTask HandleIteration(ActivityExecutionContext context)
    {
        var iterateNode = Body!;
        var end = context.Get(End);
        var currentValue = context.GetProperty<int?>(CurrentStepProperty);
        var start = context.Get(Start);
        var step = context.Get(Step);
        var inclusive = context.Get(OuterBoundInclusive);
        var increment = end >= start;

        currentValue = currentValue == null ? start : increment ? currentValue + step : currentValue - step;

        var loop =
            increment && inclusive ? currentValue <= end
            : increment && !inclusive ? currentValue < end
            : !increment && inclusive ? currentValue >= end
            : !increment && !inclusive && currentValue > end;

        if (loop)
        {
            await context.ScheduleActivityAsync(iterateNode, OnChildComplete);

            // Update internal step.
            context.SetProperty(CurrentStepProperty, currentValue);

            // Update loop variable.
            context.Set(CurrentValue, currentValue);
        }
        else
        {
            // Report activity completion.
            await context.CompleteActivityAsync();
        }
    }

    private async ValueTask OnChildComplete(ActivityExecutionContext context, ActivityExecutionContext childContext) => await HandleIteration(context);
}