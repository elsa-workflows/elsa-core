using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

[Activity("Elsa", "Flow", "Merge multiple branches into a single branch of execution.")]
public class FlowJoin : Activity, IJoinNode
{
    [Input] public Input<JoinMode> Mode { get; set; } = new(JoinMode.WaitAll);

    protected override void Execute(ActivityExecutionContext context)
    {
        // Clear any bookmarks created between this join and its most recent fork.
        var joinNode = context.ActivityNode;
        var forkNode = context.ActivityNode.Fork();

        if (forkNode == null)
            return;

        var ancestry = joinNode.AncestryTo(forkNode).ToList();

        // TODO: Remove any bookmarks created by any nodes in the ancestry path.
    }

    public bool GetShouldExecute(FlowJoinContext context)
    {
        var activityExecutionContext = context.ActivityExecutionContext;
        var mode = activityExecutionContext.Get(Mode);
        var scope = context.Scope;
        var executionCount = scope.GetExecutionCount(context.Activity);
        var inboundActivities = context.InboundActivities;
        
        if (mode == JoinMode.WaitAny)
        {
            // Only return true if we didn't execute before.
            var hasAnyInboundActivityExecuted = inboundActivities.Any(x => scope.GetExecutionCount(x) > executionCount);
            return hasAnyInboundActivityExecuted;
        }
        else
        {
            // Wait for all left-inbound activities to have executed.
            var haveAllInboundActivitiesExecuted = inboundActivities.All(x => scope.GetExecutionCount(x) > executionCount);

            return haveAllInboundActivitiesExecuted;    
        }

        
    }
}