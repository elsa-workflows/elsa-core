using System.ComponentModel;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Signals;

namespace Elsa.Activities;

[Category("Workflows")]
[Activity("Elsa", "Workflows", "Execute a set of activities in sequence.")]
public class Sequence : Container
{
    private const string CurrentIndexProperty = "CurrentIndex";
        
    public Sequence()
    {
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
            await context.SignalAsync(new ActivityCompleted());
            return;
        }
            
        var nextActivity = childActivities.ElementAt(currentIndex);
        context.PostActivity(nextActivity, OnChildCompleted);
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private async ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await HandleItemAsync(context);
    }
}