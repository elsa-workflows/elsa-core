using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityExecutionContextSchedulerStrategy : IActivityExecutionContextSchedulerStrategy
{
    /// <inheritdoc />
    public async Task ScheduleActivityAsync(ActivityExecutionContext context, IActivity? activity, ActivityExecutionContext? owner, ScheduleWorkOptions? options = null)
    {
        var activityNode = activity != null
            ? context.WorkflowExecutionContext.FindNodeByActivity(activity) ?? throw new InvalidOperationException("The specified activity is not part of the workflow.")
            : null;
        await ScheduleActivityAsync(context, activityNode, owner, options);
    }

    /// <inheritdoc />
    public async Task ScheduleActivityAsync(ActivityExecutionContext context, ActivityNode? activityNode, ActivityExecutionContext? owner = null, ScheduleWorkOptions? options = null)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        if (context.GetIsBackgroundExecution())
        {
            // Validate that the specified activity is part of the workflow.
            if (activityNode != null && !workflowExecutionContext.NodeActivityLookup.ContainsKey(activityNode.Activity))
                throw new InvalidOperationException("The specified activity is not part of the workflow.");

            var scheduledActivity = new ScheduledActivity
            {
                ActivityNodeId = activityNode?.NodeId,
                OwnerActivityInstanceId = owner?.Id,
                Options = options != null
                    ? new ScheduledActivityOptions
                    {
                        CompletionCallback = options?.CompletionCallback?.Method.Name,
                        Tag = options?.Tag,
                        ExistingActivityInstanceId = options?.ExistingActivityExecutionContext?.Id,
                        PreventDuplicateScheduling = options?.PreventDuplicateScheduling ?? false,
                        Variables = options?.Variables?.ToList(),
                        Input = options?.Input
                    }
                    : null
            };

            var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
            scheduledActivities.Add(scheduledActivity);
            context.SetBackgroundScheduledActivities(scheduledActivities);
            return;
        }

        var completionCallback = options?.CompletionCallback;
        owner ??= context;

        if (activityNode == null)
        {
            if (completionCallback != null)
            {
                context.Tag = options?.Tag;
                var completedContext = new ActivityCompletedContext(context, context);
                await completionCallback(completedContext);
            }
            else
                await owner.CompleteActivityAsync();

            return;
        }

        workflowExecutionContext.Schedule(activityNode, owner, options);
    }
}