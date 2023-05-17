using Elsa.Environments.Models;

namespace Elsa.Environments.Contracts;

/// <summary>
/// Manages environments.
/// </summary>
public interface IEnvironmentsManager
{
    /// <summary>
    /// Returns all environments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All environments.</returns>
    ValueTask<IEnumerable<ServerEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the name of the default environment.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The name of the default environment.</returns>
    ValueTask<string?> GetDefaultEnvironmentNameAsync(CancellationToken cancellationToken = default);
}