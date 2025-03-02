using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities;

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
    [Input(Description = "The items to iterate through.")]
    public Input<object> Items { get; set; } = new(Array.Empty<T>());

    /// <summary>
    /// The <see cref="IActivity"/> to execute each iteration.
    /// </summary>
    [Port]
    public IActivity Body { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var items = context.GetItemSource<T>(Items);
        var tags = new List<Guid>();
        var currentIndex = 0;
        
        await foreach (var item in items)
        {
            // For each item, declare a new variable for the work to be scheduled.
            var currentValueVariable = new Variable<T>("CurrentValue", item)
            {
                // TODO: This should be configurable, because this won't work for e.g. file streams and other non-serializable types.
                StorageDriverType = typeof(WorkflowInstanceStorageDriver)
            };

            var currentIndexVariable = new Variable<int>("CurrentIndex", currentIndex++) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) };
            var variables = new List<Variable> { currentValueVariable, currentIndexVariable };

            // Schedule a body of work for each item.
            var tag = Guid.NewGuid();
            tags.Add(tag);
            await context.ScheduleActivityAsync(Body, OnChildCompleted, tag, variables);
        }
        
        SetTagList(context, ScheduledTagsProperty, tags);
        SetTagList(context, CompletedTagsProperty, new List<Guid>());
        
        // If there were no items, we're done.
        if (tags.Count == 0)
            await context.CompleteActivityAsync();
    }

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var scheduledTags = GetTagList(targetContext, ScheduledTagsProperty);
        var completedTag = targetContext.Tag.ConvertTo<Guid>();
        var completedTags = GetTagList(targetContext, CompletedTagsProperty);
        
        completedTags.Add(completedTag);
        SetTagList(targetContext, CompletedTagsProperty, completedTags);
            
        // If not all scheduled activities have completed yet, we're not done yet.
        if (!scheduledTags.IsEqualTo(completedTags))
            return;

        // We're done, so complete the activity.
        await targetContext.CompleteActivityAsync();
    }
    
    private ICollection<Guid> GetTagList(ActivityExecutionContext context, string propertyName)
    {
        // Read the list of tags from the context using the specified property name. The value is stored as JsonArray, so we need to deserialize it.
        var jsonArray = context.GetProperty<JsonArray>(propertyName);
        return jsonArray.Select(x => x.ConvertTo<Guid>()).ToList();
    }
    
    private void SetTagList(ActivityExecutionContext context, string propertyName, ICollection<Guid> tags)
    {
        // Serialize the list of tags to a JsonArray and store it in the context using the specified property name.
        var jsonArray = JsonSerializer.SerializeToNode(tags) as JsonArray;
        context.SetProperty(propertyName, jsonArray);
    }
}