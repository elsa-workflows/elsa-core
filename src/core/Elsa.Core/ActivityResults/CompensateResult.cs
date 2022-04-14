using System;
using Elsa.Services.Compensation;
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
        var compensationService = activityExecutionContext.GetService<ICompensationService>();
        
        if (string.IsNullOrWhiteSpace(CompensableActivityId))
            compensationService.Compensate(activityExecutionContext, Exception, Message);
        else
            compensationService.Compensate(activityExecutionContext, CompensableActivityId, Exception, Message);
    }
}