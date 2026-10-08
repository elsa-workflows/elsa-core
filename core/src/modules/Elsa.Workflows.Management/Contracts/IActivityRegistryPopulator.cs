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

    /// <summary>
    /// Ensures that activity descriptors have been populated, initializing tenant-agnostic providers once.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// The default implementation preserves the existing behavior for custom populators.
    /// </remarks>
    Task EnsureRegistryPopulatedAsync(CancellationToken cancellationToken = default) =>
        PopulateRegistryAsync(cancellationToken);
}
