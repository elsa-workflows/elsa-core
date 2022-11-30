using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Control Flow", "Schedule an activity for each item in parallel.")]
public class ParallelForEach<T> : Activity
{
    private const string CollectedCountProperty = nameof(CollectedCountProperty);
    [Input] public Input<ICollection<T>> Items { get; set; } = new(Array.Empty<T>());
    [Port] public IActivity Body { get; set; } = default!;

    /// <summary>
    /// Assign a memory reference to gain access to individual branches of parallel execution from within each branch.
    /// </summary>
    public MemoryBlockReference? CurrentValue { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var items = context.Get(Items)!.Reverse().ToList();

        foreach (var item in items)
        {
            // For each item, declare a new block of memory to store the item into.
            var localVariable = new Variable<T>()
            {
                Value = item
            };

            if (CurrentValue != null)
                localVariable.Id = localVariable.Id;

            // Schedule a body of work for each item.
            await context.ScheduleActivityAsync(Body, OnChildCompleted, new[] { localVariable });
        }
    }

    private ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var itemCount = context.Get(Items)!.Count;
        var collectedCount = context.UpdateProperty<int>(CollectedCountProperty, count => count + 1);

        // Prevent next sibling from executing while not all scheduled activities have completed.
        if (collectedCount < itemCount)
            context.PreventContinuation();

        return ValueTask.CompletedTask;
    }
}