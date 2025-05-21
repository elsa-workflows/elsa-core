using System.Text.Json;
using Elsa.Resilience.Models;
using JetBrains.Annotations;

namespace Elsa.Resilience.Recorders;

[UsedImplicitly]
public class ActivityExecutionContextRetryAttemptRecorder : IRetryAttemptRecorder
{
    public Task RecordAsync(RecordRetryAttemptsContext context)
    {
        var jsonObject = JsonSerializer.SerializeToNode(context.Attempts)!;
        context.ActivityExecutionContext.Properties["RetryAttempts"] = jsonObject;
        return Task.CompletedTask;
    }
}