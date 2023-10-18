using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Signals;

/// <summary>
/// Signaled when the scheduling of a child activity was requested.
/// </summary>
public class ScheduleChildActivity
{
    /// <summary>
    /// Signaled when the scheduling of a child activity was requested.
    /// </summary>
    /// <param name="activity">The child activity to schedule.</param>
    public ScheduleChildActivity(IActivity activity)
    {
        Activity = activity;
    }

    /// <summary>
    /// Signaled when the scheduling of a child activity was requested.
    /// </summary>
    /// <param name="activityExecutionContext">The child activity execution context to schedule.</param>
    public ScheduleChildActivity(ActivityExecutionContext? activityExecutionContext)
    {
        ActivityExecutionContext = activityExecutionContext;
    }

    /// <summary>The child activity to schedule.</summary>
    public IActivity? Activity { get; init; }

    /// <summary>
    /// The child activity execution context to schedule.
    /// </summary>
    public ActivityExecutionContext? ActivityExecutionContext { get; init; }
}