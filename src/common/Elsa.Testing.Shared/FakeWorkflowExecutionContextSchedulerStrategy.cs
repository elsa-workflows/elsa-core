using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Testing.Shared;

/// <summary>
/// Bypasses validation and schedules the activity immediately.
/// </summary>
public class FakeWorkflowExecutionContextSchedulerStrategy : IWorkflowExecutionContextSchedulerStrategy
{
    public ActivityWorkItem Schedule(WorkflowExecutionContext context, ActivityNode activityNode, ActivityExecutionContext owner, ScheduleWorkOptions? options = null)
    {
        var workItem = new ActivityWorkItem(activityNode.Activity, owner);
        context.Scheduler.Schedule(workItem);
        return workItem;
    }
}