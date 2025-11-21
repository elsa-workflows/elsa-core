using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExecutionContextExtensions
{
    private const string GraphTransientProperty = "FlowGraph";

    public static IActivity? GetStartActivity(this Activities.Flowchart flowchart, string? triggerActivityId)
    {
        var activities = flowchart.Activities;

        // If there's a trigger that triggered this workflow, use that.
        var triggerActivity = triggerActivityId != null ? activities.FirstOrDefault(x => x.Id == triggerActivityId) : null;

        if (triggerActivity != null)
            return triggerActivity;

        // If an explicit Start activity was provided, use that.
        if (flowchart.Start != null)
            return flowchart.Start;

        // If there is a Start activity on the flowchart, use that.
        var startActivity = activities.FirstOrDefault(x => x is Start);

        if (startActivity != null)
            return startActivity;

        // If there's an activity marked as "Can Start Workflow", use that.
        var canStartWorkflowActivity = activities.FirstOrDefault(x => x.GetCanStartWorkflow());

        if (canStartWorkflowActivity != null)
            return canStartWorkflowActivity;

        // If there is a single activity that has no inbound connections, use that.
        var root = flowchart.GetRootActivity();

        if (root != null)
            return root;

        // If no start activity found, return the first activity.
        return activities.FirstOrDefault();
    }

    extension(ActivityExecutionContext context)
    {
        /// <summary>
        /// Checks if there is any pending work for the flowchart.
        /// </summary>
        internal bool HasPendingWork()
        {
            var flowchart = (Activities.Flowchart)context.Activity;
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var activityIds = flowchart.Activities.Select(x => x.Id).ToList();
            var children = context.Children;
            var hasRunningActivityInstances = children.Where(x => activityIds.Contains(x.Activity.Id)).Any(x => x.Status == ActivityStatus.Running);
            var hasUnconsumedTokens = flowchart.GetTokenList(context).Any(x => x is { Consumed: false, Blocked: false });
            var hasFaulted = context.HasFaultedChildren();

            var hasPendingWork = workflowExecutionContext.Scheduler.List().Any(workItem =>
            {
                var ownerInstanceId = workItem.Owner?.Id;

                if (ownerInstanceId == null)
                    return false;

                if (ownerInstanceId == context.Id)
                    return true;

                var ownerContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == ownerInstanceId);
                var ancestors = ownerContext.GetAncestors().ToList();

                return ancestors.Any(x => x == context);
            });

            return hasRunningActivityInstances || hasPendingWork || hasUnconsumedTokens || hasFaulted;
        }

        internal bool HasFaultedChildren()
        {
            return context.Children.Any(x => x.Status == ActivityStatus.Faulted);
        }

        internal FlowGraph GetFlowGraph()
        {
            // Store in TransientProperties so FlowChart is not persisted in WorkflowState
            var flowchart = (Activities.Flowchart)context.Activity;
            var startActivity = flowchart.GetStartActivity(context.WorkflowExecutionContext.TriggerActivityId);
            return context.TransientProperties.GetOrAdd(GraphTransientProperty, () => new FlowGraph(flowchart.Connections, startActivity));
        }

        internal async Task CancelInboundAncestorsAsync(IActivity activity)
        {
            if(context.Activity is not Activities.Flowchart)
                throw new InvalidOperationException("Activity context is not a flowchart.");
        
            var flowGraph = context.GetFlowGraph();
            var ancestorActivities = flowGraph.GetAncestorActivities(activity);
            var inboundActivityExecutionContexts = context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => ancestorActivities.Contains(x.Activity) && x.ParentActivityExecutionContext == context).ToList();

            // Cancel each ancestor activity.
            foreach (var activityExecutionContext in inboundActivityExecutionContexts)
            {
                await activityExecutionContext.CancelActivityAsync();
            }
        }
    }
}