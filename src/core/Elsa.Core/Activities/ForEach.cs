using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities;

[Activity("Elsa", "Control Flow", "Iterate over a set of values.")]
public class ForEach : Activity
{
    private const string CurrentIndexProperty = "CurrentIndex";
    
    /// <summary>
    /// The set of values to iterate.
    /// </summary>
    [Input] public Input<ICollection<object>> Items { get; set; } = new(Array.Empty<object>());
    
    /// <summary>
    /// The activity to execute for each iteration.
    /// </summary>
    [Outbound] public IActivity? Body { get; set; }
    
    /// <summary>
    /// The current value being iterated.
    /// </summary>
    public Variable CurrentValue { get; set; } = new();

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

        if (Body != null)
            context.PostActivity(Body, OnChildCompleted);

        // Increment index.
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        HandleIteration(context);
        return ValueTask.CompletedTask;
    }
}

public class ForEach<T> : ForEach
{
    [Input]
    public new Input<ICollection<T>> Items
    {
        get => new(base.Items.Expression, base.Items.LocationReference);
        set => base.Items = new Input<ICollection<object>>(value.Expression, value.LocationReference);
    }

    public new Variable<T> CurrentValue
    {
        get => (Variable<T>)base.CurrentValue;
        set => base.CurrentValue = value;
    }
}