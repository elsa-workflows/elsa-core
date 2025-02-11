namespace Elsa.Workflows.Management;

/// <summary>
/// Populates the <see cref="IActivityRegistry"/> with activities.
/// </summary>
public interface IActivityRegistryPopulator
{
    /// <summary>
    /// Populates the <see cref="IActivityRegistry"/> with activities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateRegistryAsync(CancellationToken cancellationToken = default);
}