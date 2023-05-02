using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Iterate over a set of values.
/// </summary>
[Activity("Elsa", "Looping", "Iterate over a set of values.")]
[PublicAPI]
public class ForEach : Activity
{
    private const string CurrentIndexProperty = "CurrentIndex";

    /// <inheritdoc />
    [JsonConstructor]
    public ForEach() : this(default, default)
    {
    }

    /// <inheritdoc />
    public ForEach([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
    {
        Behaviors.Add<BreakBehavior>(this);
    }

    /// <inheritdoc />
    public ForEach(ICollection<object> items, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Items = new Input<ICollection<object>>(items);
    }

    /// <summary>
    /// The set of values to iterate.
    /// </summary>
    [Input(Description = "The set of values to iterate.")]
    public Input<ICollection<object>> Items { get; set; } = new(Array.Empty<object>());

    /// <summary>
    /// The activity to execute for each iteration.
    /// </summary>
    [Port]
    public IActivity? Body { get; set; }

    /// <summary>
    /// The current value being iterated will be assigned to the specified <see cref="MemoryBlockReference"/>.
    /// </summary>
    [Output(Description = "Assign the current value to the specified variable.")]
    public Output<object>? CurrentValue { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Execute first iteration.
        await HandleIteration(context);
    }

    private async Task HandleIteration(ActivityExecutionContext context)
    {
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var items = context.Get(Items)!.ToList();

        if (currentIndex >= items.Count)
        {
            await context.CompleteActivityAsync();
            return;
        }

        var currentItem = items[currentIndex];
        context.Set(CurrentValue, currentItem);

        if (Body != null)
            await context.ScheduleActivityAsync(Body, OnChildCompleted);
        else
            await context.CompleteActivityAsync();

        // Increment index.
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private async ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await HandleIteration(context);
    }
}

/// <summary>
/// A strongly-typed for-each construct where <see cref="T"/> is the item type.
/// </summary>
[PublicAPI]
public class ForEach<T> : ForEach
{
    /// <inheritdoc />
    [JsonConstructor]
    public ForEach()
    {
    }

    /// <inheritdoc />
    public ForEach(Input<ICollection<T>> items) : this()
    {
        Items = items;
    }

    /// <inheritdoc />
    public ForEach(ICollection<T> items) : this(new Input<ICollection<T>>(items))
    {
    }

    /// <inheritdoc />
    public ForEach(Func<ExpressionExecutionContext, ICollection<T>> items) : this(new Input<ICollection<T>>(items))
    {
    }

    /// <summary>
    /// The items to iterate on.
    /// </summary>
    [Input]
    public new Input<ICollection<T>> Items
    {
        get => new(base.Items.Expression, base.Items.MemoryBlockReference());
        set => base.Items = new Input<ICollection<object>>(value.Expression, value.MemoryBlockReference());
    }

    /// <summary>
    /// Provides access to the current value.
    /// </summary>
    public new Output<T?> CurrentValue
    {
        get =>
            base.CurrentValue != null
                ? new(base.CurrentValue.MemoryBlockReference)
                : new();
        set => base.CurrentValue = new Output<object>(value.MemoryBlockReference);
    }
}