using System.ComponentModel;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

[Category("Workflows")]
[Activity("Elsa", "Workflows", "Execute a set of activities in sequence.")]
public class Sequence : Container
{
    private const string CurrentIndexProperty = "CurrentIndex";
        
    public Sequence()
    {
        OnSignalReceived<BreakSignal>(OnBreak);
    }

    public Sequence(params IActivity[] activities) : base(activities)
    {
    }
    
    public Sequence(ICollection<Variable> variables, params IActivity[] activities) : base(variables, activities)
    {
    }

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
        // Clear any scheduled child completion callbacks, since we no longer want to schedule any sibling. 
        context.ReceiverActivityExecutionContext.ClearCompletionCallbacks();
    }
}