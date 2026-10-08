using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides a fake implementation of the <see cref="IActivityExecutionContextSchedulerStrategy"/> interface,
/// designed for testing purposes. This class allows scheduling of activities within an activity execution context
/// and enables control over workflows in a simulated environment.
/// </summary>
public class FakeActivityExecutionContextSchedulerStrategy : IActivityExecutionContextSchedulerStrategy
{
    /// <summary>
    /// Schedules the provided activity for execution within the given activity execution context.
    /// </summary>
    /// <param name="context">The current activity execution context in which to schedule the activity.</param>
    /// <param name="activity">The activity to be scheduled. Can be null, in which case no operation is performed.</param>
    /// <param name="owner">The owner activity execution context, if any.</param>
    /// <param name="options">Optional scheduling options to influence how the activity is scheduled.</param>
    public Task ScheduleActivityAsync(ActivityExecutionContext context, IActivity? activity, ActivityExecutionContext? owner, ScheduleWorkOptions? options = null)
    {
        if (activity == null)
            return Task.CompletedTask;
        
        var activityNode = new ActivityNode(activity!, "");
        return ScheduleActivityAsync(context, activityNode, owner, options);
    }

    /// <summary>
    /// Schedules the provided activity for execution within the specified activity execution context using the given scheduling strategy.
    /// </summary>
    /// <param name="context">The activity execution context in which the activity will be scheduled.</param>
    /// <param name="activityNode">The activity node to be scheduled. If null, no operation is performed.</param>
    /// <param name="owner">The owner activity execution context, if any, that triggered the scheduling request.</param>
    /// <param name="options">Optional scheduling parameters to customize the scheduling behavior.</param>
    public Task ScheduleActivityAsync(ActivityExecutionContext context, ActivityNode? activityNode, ActivityExecutionContext? owner = null, ScheduleWorkOptions? options = null)
    {
        if (activityNode == null)
            return Task.CompletedTask;
        
        context.WorkflowExecutionContext.Schedule(activityNode!, owner ?? context, options);
        return Task.CompletedTask;
    }
}