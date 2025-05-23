using Elsa.Resilience.Models;

namespace Elsa.Resilience;

public class VoidRetryAttemptRecorder : IRetryAttemptRecorder
{
    public static VoidRetryAttemptRecorder Instance { get; } = new();
    
    public Task RecordAsync(RecordRetryAttemptsContext context)
    {
        // Send records into the void.
        return Task.CompletedTask;
    }
}