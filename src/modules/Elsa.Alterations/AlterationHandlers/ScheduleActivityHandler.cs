using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Schedules an activity for execution.
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
        
        // Check to see if there's an existing activity execution context for the activity.
        var existingActivityExecutionContext = workflowExecutionContext.ActiveActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activityId);
        
        if (existingActivityExecutionContext != null)
        {
            // If the activity is in a faulted state, reset it to Running.
            if (existingActivityExecutionContext.Status == ActivityStatus.Faulted)
                existingActivityExecutionContext.Status = ActivityStatus.Running;
            
            // Schedule the activity execution context.
            workflowExecutionContext.ScheduleActivityExecutionContext(existingActivityExecutionContext);
            context.Succeed();
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