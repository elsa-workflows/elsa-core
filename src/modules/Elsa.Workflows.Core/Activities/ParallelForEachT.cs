using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Schedule an activity for each item in parallel.
/// </summary>
/// <typeparam name="T"></typeparam>
[Activity("Elsa", "Looping", "Schedule an activity for each item in parallel.")]
public class ParallelForEach<T> : Activity
{
    private const string ScheduledTagsProperty = nameof(ScheduledTagsProperty);
    private const string CompletedTagsProperty = nameof(CompletedTagsProperty);

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

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var items = context.Get(Items)!.Reverse().ToList();
        var tags = new List<Guid>();

        foreach (var item in items)
        {
            // TODO: For each item, declare a new block of memory to store the item into.

            // Schedule a body of work for each item.
            var tag = Guid.NewGuid();
            tags.Add(tag);
            await context.ScheduleActivityAsync(Body, OnChildCompleted, tag);
        }

        context.SetProperty(ScheduledTagsProperty, tags);
        context.SetProperty(CompletedTagsProperty, new List<Guid>());
    }

    private async ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var scheduledTags = context.GetProperty<List<Guid>>(ScheduledTagsProperty)!;
        var completedTag = (Guid)childContext.Tag!;

        var completedTags = new HashSet<Guid>(context.UpdateProperty<List<Guid>>(CompletedTagsProperty, completedTags =>
        {
            completedTags!.Add(completedTag);
            return completedTags;
        }));

        // If not all scheduled activities have completed yet, we're not done yet.
        if (!scheduledTags.IsEqualTo(completedTags))
            return;

        // We're done, so complete the activity.
        await context.CompleteActivityAsync();
    }
}