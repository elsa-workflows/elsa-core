using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Execute a set of activities in parallel.
/// </summary>
[Activity("Elsa", "Workflows", "Execute a set of activities in parallel.")]
[Browsable(false)]
public class Parallel : Container
{
    private const string ScheduledChildrenProperty = "ScheduledChildren";
    
    /// <inheritdoc />
    [JsonConstructor]
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
    
    private static async ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var remainingChildren = context.UpdateProperty<int>(ScheduledChildrenProperty, scheduledChildren => scheduledChildren - 1);
        
        if (remainingChildren == 0)
            await context.CompleteActivityAsync();
    }
}