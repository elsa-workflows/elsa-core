using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

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
        DefaultValue = FlowJoinMode.WaitAny,
        UIHint = InputUIHints.DropDown
    )]
    public Input<FlowJoinMode> Mode { get; set; } = new(FlowJoinMode.WaitAny);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var flowchartContext = context.ParentActivityExecutionContext!;
        var flowchart = (Flowchart)flowchartContext.Activity;
        var inboundActivities = flowchart.Connections.LeftInboundActivities(this).ToList();
        var flowScope = flowchartContext.GetProperty(Flowchart.ScopeProperty, () => new FlowScope());
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
                    await CancelActivitiesInInboundPathAsync(flowchart, flowchartContext, context);
                    await context.CompleteActivityAsync();
                }

                break;
            }
            case FlowJoinMode.WaitAny:
            {
                await CancelActivitiesInInboundPathAsync(flowchart, flowchartContext, context);
                await context.CompleteActivityAsync();
                break;
            }
        }
    }

    private async Task CancelActivitiesInInboundPathAsync(Flowchart flowchart, ActivityExecutionContext flowchartContext, ActivityExecutionContext joinContext)
    {
        // Cancel all activities between this join activity and its most recent fork.
        var connections = flowchart.Connections;
        var workflowExecutionContext = joinContext.WorkflowExecutionContext;
        var inboundActivities = connections.LeftAncestorActivities(this).Select(x => workflowExecutionContext.FindNodeByActivity(x)).Select(x => x!.Activity).ToList();
        var inboundActivityExecutionContexts = workflowExecutionContext.ActivityExecutionContexts.Where(x => inboundActivities.Contains(x.Activity) && x.ParentActivityExecutionContext == flowchartContext).ToList();

        // Cancel each inbound activity.
        foreach (var activityExecutionContext in inboundActivityExecutionContexts)
            await activityExecutionContext.CancelActivityAsync();
    }
}