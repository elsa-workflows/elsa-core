namespace Elsa.Workflows.Signals;

/// <summary>
/// Signaled when the scheduling of a child activity was requested.
/// </summary>
public class ScheduleChildActivity
{
    /// <summary>
    /// Signaled when the scheduling of a child activity was requested.
    /// </summary>
    /// <param name="activity">The child activity to schedule.</param>
    /// <param name="input">Input to pass to the child activity.</param>
    public ScheduleChildActivity(IActivity activity, IDictionary<string, object>? input = default)
    {
        Activity = activity;
        Input = input;
    }

    /// <summary>
    /// Signaled when the scheduling of a child activity was requested.
    /// </summary>
    /// <param name="activityExecutionContext">The child activity execution context to schedule.</param>
    /// <param name="input">The scheduling options.</param>
    public ScheduleChildActivity(ActivityExecutionContext? activityExecutionContext, IDictionary<string, object>? input = default)
    {
        ActivityExecutionContext = activityExecutionContext;
        Input = input;
    }

    /// <summary>The child activity to schedule.</summary>
    public IActivity? Activity { get; init; }

    /// <summary>
    /// Input to pass to the child activity.
    /// </summary>
    public IDictionary<string, object>? Input { get; set;}

    /// <summary>
    /// The child activity execution context to schedule.
    /// </summary>
    public ActivityExecutionContext? ActivityExecutionContext { get; init; }
}