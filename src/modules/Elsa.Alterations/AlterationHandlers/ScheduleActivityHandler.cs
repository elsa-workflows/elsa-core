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
    protected override async ValueTask HandleAsync(AlterationContext context, ScheduleActivity alteration)
    {
        if (alteration.ActivityInstanceId == null && alteration.ActivityId == null)
        {
            context.Fail("Either ActivityInstanceId or ActivityId must be specified");
            return;
        }

        var workflowExecutionContext = context.WorkflowExecutionContext;
        var existingActivityExecutionContext = GetActivityExecutionContext(context, alteration);

        if (existingActivityExecutionContext != null)
        {
            // If the activity is in a faulted state, reset it to Running.
            if (existingActivityExecutionContext.Status == ActivityStatus.Faulted)
                existingActivityExecutionContext.Status = ActivityStatus.Running;

            // Schedule the activity execution context.
            var parentContext = existingActivityExecutionContext.ParentActivityExecutionContext;
            await parentContext!.SendSignalAsync(new ScheduleChildActivity(existingActivityExecutionContext));
            context.Succeed();
            return;
        }

        // Schedule a new activity instance.
        var activityId = alteration.ActivityId;

        if (activityId == null)
        {
            context.Fail("No existing activity execution context was found and no activity ID was specified");
            return;
        }

        var activityNode = workflowExecutionContext.FindNodeByActivityId(activityId);

        if (activityNode == null)
        {
            context.Fail($"Activity with ID {activityId} not found");
            return;
        }

        // Find the parent activity execution context within which to schedule the activity.
        var parentActivityContexts = workflowExecutionContext.ActivityExecutionContexts.Reverse().ToList();

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

        await parentExecutionContext.SendSignalAsync(new ScheduleChildActivity(activityNode.Activity));
        context.Succeed();
    }

    private static ActivityExecutionContext? GetActivityExecutionContext(AlterationContext context, ScheduleActivity alteration)
    {
        if (alteration.ActivityInstanceId != null)
            return context.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == alteration.ActivityInstanceId);

        return null;
    }
}