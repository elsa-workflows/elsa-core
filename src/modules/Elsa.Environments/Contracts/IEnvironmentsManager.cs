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
    Task<IEnumerable<WorkflowsEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default);
}