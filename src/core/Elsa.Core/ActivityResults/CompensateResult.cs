using System;
using System.Linq;
using Elsa.Activities.Compensation.Compensable;
using Elsa.Exceptions;
using Elsa.Services.Models;

namespace Elsa.ActivityResults;

public class CompensateResult : ActivityExecutionResult
{ 
    public CompensateResult(string message, Exception? exception = default, string? compensableActivityId = default)
    {
        Message = message;
        Exception = exception;
        CompensableActivityId = compensableActivityId;
    }

    public string Message { get; }
    public Exception? Exception { get; }
    
    /// <summary>
    /// The ID of a specific compensable activity to invoke.
    /// </summary>
    public string? CompensableActivityId { get; }

    protected override void Execute(ActivityExecutionContext activityExecutionContext)
    {
        // If the workflow contains compensable activities, schedule these instead of throwing an exception.
        Compensate(activityExecutionContext);
    }
    
    private void Compensate(ActivityExecutionContext activityExecutionContext)
    {
        if (string.IsNullOrWhiteSpace(CompensableActivityId))
            CompensateAutomatically(activityExecutionContext);
        else
            CompensateExplicitly(activityExecutionContext, CompensableActivityId);
    }

    private void CompensateExplicitly(ActivityExecutionContext activityExecutionContext, string compensableActivityId)
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
        
        activityExecutionContext.WorkflowInstance.SetMetadata("Compensated", true);
        activityExecutionContext.WorkflowExecutionContext.Cancel(Exception, Message, faultingActivityId, activityExecutionContext.Input, activityExecutionContext.Resuming);
    }

    private void CompensateAutomatically(ActivityExecutionContext activityExecutionContext)
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

        if (!hasCompensableActivities)
        {
            activityExecutionContext.WorkflowExecutionContext.Fault(Exception, Message, faultingActivityId, activityExecutionContext.Input, activityExecutionContext.Resuming);
        }
        else
        {
            activityExecutionContext.WorkflowInstance.SetMetadata("Compensated", true);
            activityExecutionContext.WorkflowExecutionContext.Cancel(Exception, Message, faultingActivityId, activityExecutionContext.Input, activityExecutionContext.Resuming);
        }
    }
}