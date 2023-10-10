using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// Merge multiple branches into a single branch of execution.
/// </summary>
[Activity("Elsa", "Branching", "Merge multiple branches into a single branch of execution.", DisplayName = "Join")]
[PublicAPI]
public class FlowJoin : Activity, IJoinNode
{
    /// <inheritdoc />
    public FlowJoin([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The join mode determines whether this activity should continue as soon as one inbound path comes in (Wait Any), or once all inbound paths have executed (Wait All).
    /// </summary>
    [Input(
        Description = "The join mode determines whether this activity should continue as soon as one inbound path comes in (Wait Any), or once all inbound paths have executed (Wait All).",
        DefaultValue = FlowJoinMode.WaitAny
    )]
    public Input<FlowJoinMode> Mode { get; set; } = new(FlowJoinMode.WaitAny);

    /// <inheritdoc />
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
            case FlowJoinMode.WaitAll:
            {
                // If all left-inbound activities have executed, complete & continue.
                var haveAllInboundActivitiesExecuted = inboundActivities.All(x => flowScope.GetExecutionCount(x) > executionCount);

                if (haveAllInboundActivitiesExecuted)
                {
                    await context.CompleteActivityAsync();
                    await ClearBookmarksAsync(flowchart, context);
                }

                break;
            }
            case FlowJoinMode.WaitAny:
            {
                await context.CompleteActivityAsync();
                await ClearBookmarksAsync(flowchart, context);
                break;
            }
        }
    }

    private async Task ClearBookmarksAsync(Flowchart flowchart, ActivityExecutionContext context)
    {
        // Cancel all activities between this join activity and its most recent fork.
        var connections = flowchart.Connections;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var inboundActivities = connections.LeftAncestorActivities(this).Select(x => workflowExecutionContext.FindNodeByActivity(x)).Select(x => x.Activity).ToList();
        var inboundActivityExecutionContexts = workflowExecutionContext.ActivityExecutionContexts.Where(x => inboundActivities.Contains(x.Activity)).ToList();

        // Cancel each inbound activity.
        foreach (var activityExecutionContext in inboundActivityExecutionContexts)
            await activityExecutionContext.CancelActivityAsync();
    }
}