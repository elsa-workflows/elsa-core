using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Sets the next activity to be scheduled on a given flowchart activity.
/// </summary>
public class ScheduleActivityHandler : AlterationHandlerBase<ScheduleActivity>
{
    /// <inheritdoc />
    protected override async ValueTask HandleAsync(AlterationHandlerContext context, ScheduleActivity alteration)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var activityId = alteration.ActivityId;
        var activity = workflowExecutionContext.FindActivityById(activityId);
        
        if (activity == null)
        {
            context.Fail($"Activity with ID {activityId} not found");
            return;
        }
        
        var activityNode = workflowExecutionContext.FindNodeByActivity(activity)!;
        var parentActivityContexts = workflowExecutionContext.ActiveActivityExecutionContexts.Reverse().ToList();

        var parentExecutionContext =
            (from ancestorNode in activityNode.Ancestors()
            from parentActivityContext in parentActivityContexts
            where parentActivityContext.Activity.Id == ancestorNode.Activity.Id
            select parentActivityContext).FirstOrDefault();
        
        if (parentExecutionContext == null)
        {
            context.Fail($"Could not find parent activity execution context for activity with ID {activityId}");
            return;
        }

        await parentExecutionContext.SendSignalAsync(new ScheduleChildActivity(activity));
        context.Succeed();
    }
}