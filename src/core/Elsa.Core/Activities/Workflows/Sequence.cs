using System.ComponentModel;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.Workflows;

[Category("Workflows")]
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

    protected override void ScheduleChildren(ActivityExecutionContext context)
    {
        HandleItem(context);
    }

    private void HandleItem(ActivityExecutionContext context)
    {
        var currentIndex = context.GetProperty<int>(CurrentIndexProperty);
        var childActivities = Activities.ToList();
            
        if (currentIndex >= childActivities.Count)
            return;
            
        var nextActivity = childActivities.ElementAt(currentIndex);
        context.SubmitActivity(nextActivity, OnChildCompleted);
        context.UpdateProperty<int>(CurrentIndexProperty, x => x + 1);
    }

    private ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        HandleItem(context);
        return ValueTask.CompletedTask;
    }
}