namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a startup task that is invoked once during application startup.
/// </summary>
public interface IStartupTask
{
    /// <summary>
    /// Invoked during application startup.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask ExecuteAsync(CancellationToken cancellationToken = default);
}