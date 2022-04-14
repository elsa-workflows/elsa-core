using System;
using System.Linq;
using Elsa.Activities.Compensation;
using Elsa.Exceptions;
using Elsa.Services.Models;

namespace Elsa.Services.Compensation;

public class CompensationService : ICompensationService
{
    public void Compensate(ActivityExecutionContext activityExecutionContext, Exception? exception = default, string? message = default)
    {
        var faultingActivityId = activityExecutionContext.ActivityId;
        var inboundPath = activityExecutionContext.WorkflowExecutionContext.GetInboundActivityPath(faultingActivityId).ToList();
        var inboundCompensableActivities = activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.GetActivities(inboundPath).Where(x => x.Type == nameof(Compensable)).ToList();
        var hasCompensableActivities = false;

        foreach (var compensableActivity in inboundCompensableActivities)
        {
            var activityData = activityExecutionContext.WorkflowInstance.ActivityData[compensableActivity.Id];

            if (!activityData.TryGetValue(nameof(Compensable.Entered), out var entered))
                continue;

            if (entered as bool? != true)
                continue;

            hasCompensableActivities = true;

            // Prepare the state of the compensable activity.
            activityData[nameof(Compensable.Compensating)] = true;

            // Schedule the compensable activity.
            activityExecutionContext.WorkflowExecutionContext.ScheduleActivity(compensableActivity.Id);
        }

        message ??= "Faulting";

        if (!hasCompensableActivities)
            return;

        activityExecutionContext.WorkflowInstance.SetMetadata("Compensated", true);
        activityExecutionContext.WorkflowExecutionContext.Cancel(exception, message, faultingActivityId, activityExecutionContext.Input, activityExecutionContext.Resuming);
    }

    public void Compensate(ActivityExecutionContext activityExecutionContext, string compensableActivityId, Exception? exception = default, string? message = default)
    {
        var faultingActivityId = activityExecutionContext.ActivityId;
        var compensableActivity = activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.GetActivity(compensableActivityId);

        if (compensableActivity == null)
            throw new WorkflowException($"No activity with ID {compensableActivityId} could be found");

        var activityData = activityExecutionContext.WorkflowInstance.ActivityData[compensableActivity.Id];

        // Prepare the state of the compensable activity.
        activityData[nameof(Compensable.Compensating)] = true;

        // Schedule the compensable activity.
        activityExecutionContext.WorkflowExecutionContext.ScheduleActivity(compensableActivity.Id);

        message ??= "Faulting";
        activityExecutionContext.WorkflowInstance.SetMetadata("Compensated", true);
        activityExecutionContext.WorkflowExecutionContext.Cancel(exception, message, faultingActivityId, activityExecutionContext.Input, activityExecutionContext.Resuming);
    }

    public void Confirm(ActivityExecutionContext activityExecutionContext, string compensableActivityId)
    {
        var compensableActivity = activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.GetActivity(compensableActivityId);

        if (compensableActivity == null)
            throw new WorkflowException($"No activity with ID {compensableActivityId} could be found");

        var activityData = activityExecutionContext.WorkflowInstance.ActivityData[compensableActivity.Id];

        // Prepare the state of the compensable activity.
        activityData[nameof(Compensable.Confirming)] = true;

        // Schedule the compensable activity.
        activityExecutionContext.WorkflowExecutionContext.ScheduleActivity(compensableActivity.Id);
    }
}