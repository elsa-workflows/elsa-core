using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Behaviors;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities;

/// <summary>
/// A strongly-typed for-each construct where <see cref="T"/> is the item type.
/// </summary>
public class ForEach<T> : Activity
{
    private const string CurrentIndexProperty = "CurrentIndex";

    /// <inheritdoc />
    public ForEach(Func<ExpressionExecutionContext, ICollection<T>> @delegate, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<ICollection<T>>(@delegate), source, line)
    {
    }
    
    /// <inheritdoc />
    public ForEach(Func<ICollection<T>> @delegate, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<ICollection<T>>(@delegate), source, line)
    {
    }

    /// <inheritdoc />
    public ForEach(ICollection<T> items, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<ICollection<T>>(items), source, line)
    {
    }
    
    /// <inheritdoc />
    public ForEach(Input<ICollection<T>> items, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Items = items;
    }
    
    /// <inheritdoc />
    public ForEach([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
    {
        Behaviors.Add<BreakBehavior>(this);
    }

    /// <summary>
    /// The set of values to iterate.
    /// </summary>
    [Input(Description = "The set of values to iterate.")]
    public Input<ICollection<T>>? Items { get; set; }
    
    /// <summary>
    /// The source of values to iterate.
    /// </summary>
    [Input(Description = "The set of values to iterate.")]
    public Input<IAsyncEnumerable<T>>? ItemSource { get; set; }

    /// <summary>
    /// The activity to execute for each iteration.
    /// </summary>
    [Port]
    public IActivity? Body { get; set; }

    /// <summary>
    /// The current value being iterated will be assigned to the specified <see cref="MemoryBlockReference"/>.
    /// </summary>
    [Output(Description = "Assign the current value to the specified variable.")]
    public Output<T>? CurrentValue { get; set; }

    /// <inheritdoc />
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
        
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var currentValueTuple = await GetCurrentValueAsync(context, currentIndex);

        if (!currentValueTuple.Exists)
        {
            await context.CompleteActivityAsync();
            return;
        }
        
        var currentValue = currentValueTuple.Value;
        
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
            await context.CompleteActivityAsync();

        // Increment index.
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private async Task<(T Value, bool Exists)> GetCurrentValueAsync(ActivityExecutionContext context, int currentIndex)
    {
        var items = context.Get(Items)?.ToList();

        if (items != null)
        {
            return (currentIndex >= items.Count ? (default, false) : (items[currentIndex], true))!;
        }
        
        var itemSource = context.Get(ItemSource);
        
        if(itemSource != null)
        {
            await using var enumerator = itemSource.GetAsyncEnumerator();
            
            // Move the cursor to the current index.
            for (var i = 0; i < currentIndex; i++)
                await enumerator.MoveNextAsync();
            
            var hasNext = await enumerator.MoveNextAsync();
            
            if(!hasNext)
                return (default, false)!;

            return (enumerator.Current, true);
        }
        
        return (default, false)!;
    }

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        await HandleIteration(context.TargetContext);
    }
}