using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows;

/// <summary>
/// Defines a strategy for scheduling activities in a workflow execution context.
/// </summary>
public interface IActivityExecutionContextSchedulerStrategy
{
    /// <summary>
    /// Schedules an activity for execution based on the specified context, activity, owner, and scheduling options.
    /// </summary>
    /// <param name="context">The execution context of the activity.</param>
    /// <param name="activity">The activity to be scheduled. Can be null if no specific activity is targeted.</param>
    /// <param name="owner">The context of the owning activity, if applicable. Can be null.</param>
    /// <param name="options">Optional scheduling options for the activity.</param>
    Task ScheduleActivityAsync(
        ActivityExecutionContext context,
        IActivity? activity,
        ActivityExecutionContext? owner,
        ScheduleWorkOptions? options = null);

    /// <summary>
    /// Schedules an activity node for execution within the specified activity execution context.
    /// </summary>
    /// <param name="context">The execution context in which the activity node will be scheduled.</param>
    /// <param name="activityNode">The activity node to be scheduled. Can be null if no specific node is targeted.</param>
    /// <param name="owner">The context of the owning activity execution, if applicable. Can be null.</param>
    /// <param name="options">Optional settings for scheduling the activity node.</param>
    Task ScheduleActivityAsync(
        ActivityExecutionContext context,
        ActivityNode? activityNode,
        ActivityExecutionContext? owner = null,
        ScheduleWorkOptions? options = null);
}