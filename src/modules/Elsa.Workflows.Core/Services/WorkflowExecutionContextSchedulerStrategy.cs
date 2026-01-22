using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows;

/// <inheritdoc />
public class WorkflowExecutionContextSchedulerStrategy : IWorkflowExecutionContextSchedulerStrategy
{
    /// <inheritdoc />
    public ActivityWorkItem Schedule(WorkflowExecutionContext context, ActivityNode activityNode, ActivityExecutionContext owner, ScheduleWorkOptions? options = null)
    {
        // Validate that the specified activity is part of the workflow.
        if (!context.NodeActivityLookup.ContainsKey(activityNode.Activity))
            throw new InvalidOperationException("The specified activity is not part of the workflow.");

        var scheduler = context.Scheduler;

        if (options?.PreventDuplicateScheduling == true)
        {
            // Check if the activity is already scheduled for the specified owner.
            var existingWorkItem = scheduler.Find(x => x.Activity.NodeId == activityNode.NodeId && x.Owner == owner);

            if (existingWorkItem != null)
                return existingWorkItem;
        }

        var activity = activityNode.Activity;
        var tag = options?.Tag;

        // Use explicit SchedulingActivityExecutionId from options, or fall back to ambient context
        var schedulingActivityExecutionId = options?.SchedulingActivityExecutionId ?? context.CurrentSchedulingActivityExecutionId;

        // Use explicit SchedulingWorkflowInstanceId from options, or fall back to ambient context
        var schedulingWorkflowInstanceId = options?.SchedulingWorkflowInstanceId ?? context.CurrentSchedulingWorkflowInstanceId;

        var workItem = new ActivityWorkItem(
            activity,
            owner,
            tag,
            options?.Variables,
            options?.ExistingActivityExecutionContext,
            options?.Input,
            schedulingActivityExecutionId,
            schedulingWorkflowInstanceId);
        var completionCallback = options?.CompletionCallback;

        context.AddCompletionCallback(owner, activityNode, completionCallback, tag);
        scheduler.Schedule(workItem);

        return workItem;
    }
}