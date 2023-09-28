using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;

namespace Elsa.Alterations.Alterations;

/// <summary>
/// Sets the next activity to be scheduled on a given container activity.
/// </summary>
public class AdjustExecutionPointer : AlterationBase
{
    /// <summary>
    /// The Id of the container activity.
    /// </summary>
    public string ContainerActivityId { get; set; } = default!;
    
    /// <summary>
    /// The Id of the next activity to be scheduled.
    /// </summary>
    public string NextActivityId { get; set; } = default!;

    /// <inheritdoc />
    public override ValueTask ApplyAsync(AlterationContext context, CancellationToken cancellationToken = default)
    {
        var flowchartId = ContainerActivityId;
        var flowchart = (Flowchart?)context.WorkflowExecutionContext.FindActivityByActivityId(flowchartId);
        
        if (flowchart == null)
        {
            context.Fail($"Flowchart with ID {flowchartId} not found");
            return default;
        }
        
        // Check to see if there's already an activity execution context.
        var flowchartActivityExecutionContext = context.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == flowchartId);
        
        // If there's already an activity execution context, then we can just use that.
        if (flowchartActivityExecutionContext != null)
        {
            // TODO: Also pass in next activity ID as a parameter to the flowchart activity. 
            context.WorkflowExecutionContext.ScheduleActivityExecutionContext(flowchartActivityExecutionContext);
        }
        
        return default;
    }
}