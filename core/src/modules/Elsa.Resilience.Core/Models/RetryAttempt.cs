using Elsa.Workflows;

namespace Elsa.Resilience.Models;

public record RetryAttempt(ActivityExecutionContext ActivityExecutionContext, int AttemptNumber, TimeSpan RetryDelay, object? Result, Exception? Exception)
{
    internal static readonly object RetriesKey = new();
}