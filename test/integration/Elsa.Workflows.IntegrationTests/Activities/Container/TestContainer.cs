namespace Elsa.Workflows.IntegrationTests.Activities.Container;

/// <summary>
/// Concrete implementation of Container for testing that schedules children sequentially.
/// </summary>
public class TestContainer : Elsa.Workflows.Activities.Container
{
    private const string CurrentIndexProperty = "CurrentIndex";

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

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        await HandleItemAsync(context.TargetContext);
    }
}