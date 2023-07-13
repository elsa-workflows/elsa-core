using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Schedule an activity for each item in parallel.
/// </summary>
/// <typeparam name="T"></typeparam>
[Activity("Elsa", "Looping", "Schedule an activity for each item in parallel.")]
[PublicAPI]
public class ParallelForEach<T> : CodeActivity
{
    private const string CollectedCountProperty = nameof(CollectedCountProperty);

    /// <inheritdoc />
    [JsonConstructor]
    public ParallelForEach()
    {
    }

    /// <inheritdoc />
    public ParallelForEach([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The items to iterate.
    /// </summary>
    [Input(Description = "The items to iterate.")]
    public Input<ICollection<T>> Items { get; set; } = new(Array.Empty<T>());

    /// <summary>
    /// The <see cref="IActivity"/> to execute each iteration.
    /// </summary>
    [Port]
    public IActivity Body { get; set; } = default!;

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