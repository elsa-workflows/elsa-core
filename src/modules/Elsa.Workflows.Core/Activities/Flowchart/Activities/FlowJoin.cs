using Elsa.Common.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

[Activity("Elsa", "Flow", "Merge multiple branches into a single branch of execution.")]
public class FlowJoin : ActivityBase, IJoinNode
{
    [Input] public Input<JoinMode> Mode { get; set; } = new(JoinMode.WaitAll);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var flowchartExecutionContext = context.ParentActivityExecutionContext!;
        var flowchart = (Flowchart)flowchartExecutionContext.Activity;
        var inboundActivities = flowchart.Connections.LeftInboundActivities(this).ToList();
        var flowScope = flowchartExecutionContext.GetProperty<FlowScope>(Flowchart.ScopeProperty)!;
        var executionCount = flowScope.GetExecutionCount(this);
        var mode = context.Get(Mode);

        switch (mode)
        {
            case JoinMode.WaitAll:
                // If all left-inbound activities have executed, complete & continue.
                var haveAllInboundActivitiesExecuted = inboundActivities.All(x => flowScope.GetExecutionCount(x) > executionCount);

                if (haveAllInboundActivitiesExecuted)
                    await context.CompleteActivityAsync();
                break;
            case JoinMode.WaitAny:
                // Only complete if we haven't already executed.
                var alreadyExecuted = inboundActivities.Max(x => flowScope.GetExecutionCount(x)) == executionCount;

                if (!alreadyExecuted)
                {
                    await context.CompleteActivityAsync();
                    ClearBookmarks(flowchart, context);
                }
                break;
        }
    }

    private void ClearBookmarks(Flowchart flowchart, ActivityExecutionContext context)
    {
        // Clear any bookmarks created between this join and its most recent fork.
        var connections = flowchart.Connections;
        var inboundActivities = connections.LeftInboundActivities(this).Select(x => x.Id).ToList();
        context.WorkflowExecutionContext.Bookmarks.RemoveWhere(x => inboundActivities.Contains(x.ActivityId));
    }
}