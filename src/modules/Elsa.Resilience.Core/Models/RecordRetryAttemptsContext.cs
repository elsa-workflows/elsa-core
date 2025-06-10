using Elsa.Resilience.Entities;
using Elsa.Workflows;

namespace Elsa.Resilience.Models;

public record RecordRetryAttemptsContext(ActivityExecutionContext ActivityExecutionContext, ICollection<RetryAttemptRecord> Attempts, CancellationToken CancellationToken);