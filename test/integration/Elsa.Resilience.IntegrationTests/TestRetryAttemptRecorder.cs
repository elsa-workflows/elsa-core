using Elsa.Resilience.Entities;
using Elsa.Resilience.Models;

namespace Elsa.Resilience.IntegrationTests;

public class TestRetryAttemptRecorder : IRetryAttemptRecorder
{
    public IList<RetryAttemptRecord> Attempts { get; } = new List<RetryAttemptRecord>();

    public Task RecordAsync(RecordRetryAttemptsContext context)
    {
        foreach (var record in context.Attempts)
            Attempts.Add(record);
        return Task.CompletedTask;
    }
}
