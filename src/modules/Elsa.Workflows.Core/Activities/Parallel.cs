using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Execute a set of activities in parallel.
/// </summary>
[Activity("Elsa", "Workflows", "Execute a set of activities in parallel.")]
[Browsable(false)] // Hidden from the designer until we have Sequential activity designer support.
public class Parallel : Container
{
    private const string ScheduledChildrenProperty = "ScheduledChildren";

    /// <inheritdoc />
    public Parallel([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    public Parallel(params IActivity[] activities) : this()
    {
        Activities = activities;
    }

    /// <inheritdoc />
    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        // If there are no activities, complete immediately
        if (Activities.Count == 0)
        {
            await context.CompleteActivityAsync();
            return;
        }

        context.SetProperty(ScheduledChildrenProperty, Activities.Count);

        // For Parallel, all children are scheduled by the parent activity (this), so the scheduling activity is the Parallel itself
        var options = new ScheduleWorkOptions
        {
            CompletionCallback = OnChildCompleted,
            SchedulingActivityExecutionId = context.Id
        };

        foreach (var activity in Activities)
            await context.ScheduleActivityAsync(activity, options);
    }
    
    private static async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        var remainingChildren = context.TargetContext.UpdateProperty<int>(ScheduledChildrenProperty, scheduledChildren => scheduledChildren - 1);
        
        if (remainingChildren == 0)
            await context.TargetContext.CompleteActivityAsync();
    }
}