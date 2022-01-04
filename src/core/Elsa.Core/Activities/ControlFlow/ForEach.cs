using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class ForEach<T> : Activity
{
    [Input] public Input<ICollection<T>> Items { get; set; } = new(Array.Empty<T>());
    [Outbound] public IActivity Body { get; set; } = default!;
    public Variable<T> CurrentValue { get; set; } = new();
    private const string CurrentIndexProperty = "CurrentIndex";

    protected override void Execute(ActivityExecutionContext context)
    {
        // Declare looping variable.
        context.ExpressionExecutionContext.Register.Declare(CurrentValue);
            
        // Execute first iteration.
        HandleIteration(context);
    }

    private void HandleIteration(ActivityExecutionContext context)
    {
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var items = context.Get(Items)!.ToList();

        if (currentIndex >= items.Count)
            return;

        var currentItem = items[currentIndex];
        CurrentValue.Set(context, currentItem);
        context.ScheduleActivity(Body, OnChildCompleted);
            
        // Increment index.
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        HandleIteration(context);
        return ValueTask.CompletedTask;
    }
}