using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Execute a set of activities in sequence.
/// </summary>
[Category("Workflows")]
[Browsable(false)]
[Activity("Elsa", "Workflows", "Execute a set of activities in sequence.")]
public class Sequence : Container
{
    private const string CurrentIndexProperty = "CurrentIndex";

    /// <inheritdoc />
    [JsonConstructor]
    public Sequence([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        OnSignalReceived<BreakSignal>(OnBreak);
    }
    
    /// <inheritdoc />
    public Sequence(params IActivity[] activities) : this()
    {
        Activities = activities;
    }

    /// <inheritdoc />
    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        await HandleItemAsync(context);
    }

    private async ValueTask HandleItemAsync(ActivityExecutionContext context)
    {
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var childActivities = Activities.ToList();
            
        if (currentIndex >= childActivities.Count)
        {
            await context.CompleteActivityAsync();
            return;
        }
            
        var nextActivity = childActivities.ElementAt(currentIndex);
        await context.ScheduleActivityAsync(nextActivity, OnChildCompleted);
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private async ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await HandleItemAsync(context);
    }
    
    private void OnBreak(BreakSignal signal, SignalContext context)
    {
        // Clear any scheduled child completion callbacks, since we no longer want to schedule any siblings. 
        context.ReceiverActivityExecutionContext.ClearCompletionCallbacks();
    }
}