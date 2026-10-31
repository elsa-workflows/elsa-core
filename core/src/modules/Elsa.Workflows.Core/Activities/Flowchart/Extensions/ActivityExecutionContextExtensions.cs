using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExecutionContextExtensions
{
    private const string GraphTransientProperty = "FlowGraph";

    extension(Activities.Flowchart flowchart)
    {
        public IActivity? GetStartActivity(string? triggerActivityId)
        {
            return flowchart.GetTriggerActivity(triggerActivityId)
                   ?? flowchart.Start
                   ?? flowchart.GetExplicitStartActivity()
                   ?? flowchart.GetCanStartWorkflowActivity()
                   ?? flowchart.GetRootActivity()
                   ?? flowchart.Activities.FirstOrDefault();
        }

        /// <summary>
        /// Gets the activity that was triggered by the specified trigger activity ID.
        /// </summary>
        public IActivity? GetTriggerActivity(string? triggerActivityId)
        {
            return triggerActivityId == null 
                ? null : 
                flowchart.Activities.FirstOrDefault(x => x.Id == triggerActivityId);
        }

        /// <summary>
        /// Gets the explicit Start activity from the flowchart.
        /// </summary>
        public IActivity? GetExplicitStartActivity()
        {
            return flowchart.Activities.FirstOrDefault(x => x is Start);
        }

        /// <summary>
        /// Gets the first activity marked as "Can Start Workflow".
        /// </summary>
        public IActivity? GetCanStartWorkflowActivity()
        {
            return flowchart.Activities.FirstOrDefault(x => x.GetCanStartWorkflow());
        }
    }

    extension(ActivityExecutionContext context)
    {
        /// <summary>
        /// Checks if there is any pending work for the flowchart.
        /// </summary>
        public bool HasPendingWork()
        {
            return context.HasRunningActivityInstances()
                   || context.HasScheduledWork()
                   || context.HasUnconsumedTokens()
                   || context.HasFaultedChildren();
        }

        public FlowGraph GetFlowGraph()
        {
            // Store in TransientProperties so FlowChart is not persisted in WorkflowState
            var flowchart = (Activities.Flowchart)context.Activity;
            var startActivity = flowchart.GetStartActivity(context.WorkflowExecutionContext.TriggerActivityId);
            return context.TransientProperties.GetOrAdd(GraphTransientProperty, () => new FlowGraph(flowchart.Connections, startActivity));
        }

        public async Task CancelInboundAncestorsAsync(IActivity activity)
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
        
        /// <summary>
        /// Checks if the flowchart has any unconsumed tokens.
        /// </summary>
        public bool HasUnconsumedTokens()
        {
            var flowchart = (Activities.Flowchart)context.Activity;
            return flowchart.GetTokenList(context).Any(x => x is { Consumed: false, Blocked: false });
        }

        /// <summary>
        /// Checks if the flowchart has any running activity instances.
        /// </summary>
        public bool HasRunningActivityInstances()
        {
            var flowchart = (Activities.Flowchart)context.Activity;
            var activityIds = flowchart.Activities.Select(x => x.Id).ToList();
            var children = context.Children;
            return children.Where(x => activityIds.Contains(x.Activity.Id)).Any(x => x.Status == ActivityStatus.Running);
        }

        /// <summary>
        /// Checks if the flowchart has any scheduled work items.
        /// </summary>
        public bool HasScheduledWork()
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;

            return workflowExecutionContext.Scheduler.List().Any(workItem =>
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
        }

        public bool HasFaultedChildren()
        {
            return context.Children.Any(x => x.Status == ActivityStatus.Faulted);
        }
    }
}