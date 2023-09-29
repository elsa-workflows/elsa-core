using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;

namespace Elsa.Alterations;

/// <summary>
/// Sets the next activity to be scheduled on a given flowchart activity.
/// </summary>
public class ScheduleFlowchartActivity : AlterationBase
{
    /// <summary>
    /// The Id of the container activity.
    /// </summary>
    public string FlowchartId { get; set; } = default!;

    /// <summary>
    /// The Id of the next activity to be scheduled.
    /// </summary>
    public string NextActivityId { get; set; } = default!;

    /// <inheritdoc />
    public override ValueTask ApplyAsync(AlterationContext context, CancellationToken cancellationToken = default)
    {
        var flowchartId = FlowchartId;
        var flowchart = (Flowchart?)context.WorkflowExecutionContext.FindActivityByActivityId(flowchartId);

        if (flowchart == null)
        {
            context.Fail($"Flowchart with ID {flowchartId} not found");
            return default;
        }

        ScheduleActivity(context, flowchart);

        return default;
    }

    private void ScheduleActivity(AlterationContext context, Flowchart activity)
    {
        // Check to see if there's already an activity execution context.
        var flowchartActivityExecutionContext = context.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activity.Id);

        var input = new Dictionary<string, object> { ["NextActivityId"] =  NextActivityId};

        // If there's already an activity execution context, then we can just use that.
        if (flowchartActivityExecutionContext != null)
        {
            context.WorkflowExecutionContext.ScheduleActivityExecutionContext(flowchartActivityExecutionContext, input);
            return;
        }

        // Otherwise, we need to schedule a new activity execution context.
        context.WorkflowExecutionContext.ScheduleActivity(activity, input);
    }
}