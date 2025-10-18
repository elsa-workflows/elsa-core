using Elsa.Workflows;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionContext"/> to enhance its functionality.
/// </summary>
public static class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Determines whether the specified activity is scheduled in the workflow execution context.
    /// </summary>
    /// <param name="activityExecutionContext">The activity execution context instance used for checking the scheduled activity.</param>
    /// <param name="activity">The activity to check for scheduling.</param>
    /// <returns>True if the specified activity is scheduled; otherwise, false.</returns>
    public static bool HasScheduledActivity(this ActivityExecutionContext activityExecutionContext, IActivity activity)
    {
        return activityExecutionContext.WorkflowExecutionContext.Scheduler.Find(x => x.Activity == activity) != null;
    }
}