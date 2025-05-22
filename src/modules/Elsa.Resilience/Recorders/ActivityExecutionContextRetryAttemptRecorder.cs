using System.Text.Json;
using Elsa.Resilience.Entities;
using Elsa.Resilience.Models;
using Elsa.Workflows;
using Elsa.Workflows.State;
using JetBrains.Annotations;

namespace Elsa.Resilience.Recorders;

[UsedImplicitly]
public class ActivityExecutionContextRetryAttemptRecorder(IIdentityGenerator identityGenerator) : IRetryAttemptRecorder
{
    public Task RecordAsync(RecordRetryAttemptsContext context)
    {
        var records = Map(context.ActivityExecutionContext, context.Attempts);
        context.ActivityExecutionContext.Properties["RetryAttempts"] = records;
        return Task.CompletedTask;
    }

    private ICollection<RetryAttemptRecord> Map(ActivityExecutionContext activityExecutionContext, ICollection<RetryAttempt> attempts)
    {
        return attempts.Select(x => Map(activityExecutionContext, x)).ToList();
    }

    private RetryAttemptRecord Map(ActivityExecutionContext activityExecutionContext, RetryAttempt attempt)
    {
        return new()
        {
            Id = identityGenerator.GenerateId(),
            ActivityInstanceId = activityExecutionContext.Id,
            ActivityId = activityExecutionContext.Activity.Id,
            WorkflowInstanceId = activityExecutionContext.WorkflowExecutionContext.Id,
            AttemptNumber = attempt.AttemptNumber,
            RetryDelay = attempt.RetryDelay,
            Exception = attempt.Exception != null ? ExceptionState.FromException(attempt.Exception) : null,
            Result = attempt.Result != null ? JsonSerializer.Serialize(attempt.Result) : null
        };
    }
}