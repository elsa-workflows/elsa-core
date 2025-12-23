namespace Elsa.Resilience.Contracts;

/// <summary>
/// Service for detecting whether exceptions are transient and may be resolved by retrying.
/// </summary>
public interface ITransientExceptionDetectionService
{
    /// <summary>
    /// Determines whether the specified exception is transient.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is transient; otherwise, false.</returns>
    bool IsTransient(Exception exception);
}
