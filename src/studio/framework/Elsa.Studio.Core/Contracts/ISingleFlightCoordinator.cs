namespace Elsa.Studio.Contracts;

/// <summary>
/// Coordinates operations to prevent multiple concurrent executions.
/// </summary>
public interface ISingleFlightCoordinator
{
    /// <summary>
    /// Ensures only one operation runs at a time for the current scope.
    /// </summary>
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
