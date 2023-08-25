using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Execute a set of activities in parallel.
/// </summary>
[Activity("Elsa", "Workflows", "Execute a set of activities in parallel.")]
[Browsable(false)] // Hidden from the designer until we have Sequential activity designer support.
public class Parallel : Container
{
    private const string ScheduledChildrenProperty = "ScheduledChildren";

    /// <inheritdoc />
    public Parallel([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
        context.SetProperty(ScheduledChildrenProperty, Activities.Count);
        
        foreach (var activity in Activities) 
            await context.ScheduleActivityAsync(activity, OnChildCompleted);
    }
    
    private static async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        var remainingChildren = context.TargetContext.UpdateProperty<int>(ScheduledChildrenProperty, scheduledChildren => scheduledChildren - 1);
        
        if (remainingChildren == 0)
            await context.TargetContext.CompleteActivityAsync();
    }
}