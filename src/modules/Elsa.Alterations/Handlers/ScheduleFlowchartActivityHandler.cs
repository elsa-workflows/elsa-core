using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Sets the next activity to be scheduled on a given flowchart activity.
/// </summary>
public class ScheduleFlowchartActivityHandler : AlterationHandlerBase<ScheduleFlowchartActivity>
{
    /// <inheritdoc />
    protected override ValueTask HandleAsync(AlterationHandlerContext context, ScheduleFlowchartActivity alteration)
    {
        var flowchartId = alteration.FlowchartId;
        var flowchart = (Flowchart?)context.WorkflowExecutionContext.FindActivityByActivityId(flowchartId);

        if (flowchart == null)
        {
            context.Fail($"Flowchart with ID {flowchartId} not found");
            return default;
        }

        ScheduleActivity(context, alteration, flowchart);

        return default;
    }

    private void ScheduleActivity(AlterationHandlerContext handlerContext, ScheduleFlowchartActivity alteration, Flowchart activity)
    {
        // Check to see if there's already an activity execution context.
        var flowchartActivityExecutionContext = handlerContext.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activity.Id);

        var input = new Dictionary<string, object> { ["NextActivityId"] = alteration.NextActivityId };

        // If there's already an activity execution context, then we can just use that.
        if (flowchartActivityExecutionContext != null)
        {
            handlerContext.WorkflowExecutionContext.ScheduleActivityExecutionContext(flowchartActivityExecutionContext, input);
            return;
        }

        // Otherwise, we need to schedule a new activity execution context.
        handlerContext.WorkflowExecutionContext.ScheduleActivity(activity, input);
    }
}