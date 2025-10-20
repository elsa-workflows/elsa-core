using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Testing.Shared;

/// <summary>
/// Bypasses validation and schedules the activity immediately.
/// </summary>
public class FakeWorkflowExecutionContextSchedulerStrategy : IWorkflowExecutionContextSchedulerStrategy
{
    /// <summary>
    /// Schedules an activity within the workflow execution context.
    /// </summary>
    /// <param name="context">The workflow execution context within which the activity will be scheduled.</param>
    /// <param name="activityNode">The activity node representing the activity to be scheduled.</param>
    /// <param name="owner">The execution context owning the activity execution.</param>
    /// <param name="options">Optional scheduling options for configuring the behavior of the scheduling process.</param>
    /// <returns>Returns an <see cref="ActivityWorkItem"/> representing the scheduled activity work item.</returns>
    public ActivityWorkItem Schedule(WorkflowExecutionContext context, ActivityNode activityNode, ActivityExecutionContext owner, ScheduleWorkOptions? options = null)
    {
        var workItem = new ActivityWorkItem(activityNode.Activity, owner);
        context.Scheduler.Schedule(workItem);
        return workItem;
    }
}