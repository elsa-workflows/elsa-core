namespace Elsa.Resilience.Contracts;

/// <summary>
/// Defines a contract for detecting whether an exception is transient and may be resolved by retrying.
/// </summary>
public interface ITransientExceptionDetector
{
    /// <summary>
    /// Determines whether the specified exception is transient.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is transient; otherwise, false.</returns>
    bool IsTransient(Exception exception);
}
