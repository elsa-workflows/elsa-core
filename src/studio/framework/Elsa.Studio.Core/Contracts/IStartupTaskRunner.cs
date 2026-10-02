namespace Elsa.Studio.Contracts;

/// <summary>
/// Runs startup tasks.
/// </summary>
public interface IStartupTaskRunner
{
    /// <summary>
    /// Runs startup tasks.
    /// </summary>
    Task RunStartupTasksAsync(CancellationToken cancellationToken = default);
}