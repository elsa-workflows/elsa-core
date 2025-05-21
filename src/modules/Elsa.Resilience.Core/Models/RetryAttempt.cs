namespace Elsa.Resilience.Models;

public record RetryAttempt(int AttemptNumber, TimeSpan RetryDelay, object? Result, Exception? Exception)
{
    internal static object RetriesKey = new();
}