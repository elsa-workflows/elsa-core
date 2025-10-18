using Elsa.Workflows;

namespace Elsa.Testing.Shared;

public static class ActivityExecutionContextExtensions
{
    public static bool HasScheduledActivity(this ActivityExecutionContext activityExecutionContext, IActivity activity)
    {
        return activityExecutionContext.WorkflowExecutionContext.Scheduler.Find(x => x.Activity == activity) != null;
    }
}