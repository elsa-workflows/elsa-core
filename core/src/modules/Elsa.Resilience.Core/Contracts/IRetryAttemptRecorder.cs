using Elsa.Resilience.Models;

namespace Elsa.Resilience;

public interface IRetryAttemptRecorder
{
    Task RecordAsync(RecordRetryAttemptsContext context);
}