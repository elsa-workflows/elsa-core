using Azure.ResourceManager;

namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// Provides an <see cref="ArmClient"/> instance.
/// </summary>
public interface IArmClientProvider
{
    /// <summary>
    /// Creates an <see cref="ArmClient"/> instance.
    /// </summary>
    /// <returns>An <see cref="ArmClient"/> instance.</returns>
    ValueTask<ArmClient> CreateClientAsync(CancellationToken cancellationToken = default);
}