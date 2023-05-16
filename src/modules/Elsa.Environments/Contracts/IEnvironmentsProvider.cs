using Elsa.Environments.Models;

namespace Elsa.Environments.Contracts;

/// <summary>
/// Provides environments to the system.
/// </summary>
public interface IEnvironmentsProvider
{
    /// <summary>
    /// Returns a list of workflow environments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow environments.</returns>
    ValueTask<IEnumerable<ServerEnvironment>> GetEnvironmentsAsync(CancellationToken cancellationToken = default);
}