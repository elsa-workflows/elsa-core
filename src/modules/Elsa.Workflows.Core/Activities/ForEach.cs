using Elsa.Workflows.Attributes;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Iterate over a set of values.
/// </summary>
[Activity("Elsa", "Looping", "Iterate over a set of values.")]
public class ForEach : Activity
{
    private const string _currentIndexProperty = "CurrentIndex";

    /// <summary>
    /// The set of values to iterate.
    /// </summary>
    [Input(Description = "The set of values to iterate.")]
    public Variable Items { get; set; } = default!;

    /// <summary>
    /// The activity to execute for each iteration.
    /// </summary>
    [Port]
    public IActivity Body { get; set; }

    /// <summary>
    /// The current value being iterated will be assigned to the specified <see cref="MemoryBlockReference"/>.
    /// </summary>
    [Output(Description = "Assign the current value to the specified variable.")]
    public Output CurrentValue { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Execute first iteration.
        await HandleIteration(context);
    }

    private async Task HandleIteration(ActivityExecutionContext context)
    {
        var isBreaking = context.GetIsBreaking();

        if (isBreaking)
        {
            await context.CompleteActivityAsync();
            return;
        }

        var currentIndex = context.GetProperty<int>(_currentIndexProperty);

        List<object> items = (context.Get(Items) as IEnumerable<object>).ToList();

        if (currentIndex >= items.Count)
        {
            await context.CompleteActivityAsync();
            return;
        }

        var currentValue = items[currentIndex];
        context.Set(CurrentValue, currentValue);

        if (Body != null)
        {
            var variables = new[]
            {
                new Variable("CurrentIndex", currentIndex),
                new Variable("CurrentValue", currentValue)
            };
            await context.ScheduleActivityAsync(Body, OnChildCompleted, variables: variables);
        }
        else
        {
            await context.CompleteActivityAsync();
        }

        // Increment index.
        context.UpdateProperty<int>(_currentIndexProperty, x => x + 1);
    }

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        await HandleIteration(context.TargetContext);
    }
}