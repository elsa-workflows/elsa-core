using Elsa.Workflows;

namespace Elsa.Resilience.Models;

public record RecordRetryAttemptsContext(ActivityExecutionContext ActivityExecutionContext, ICollection<RetryAttempt> Attempts, CancellationToken CancellationToken);