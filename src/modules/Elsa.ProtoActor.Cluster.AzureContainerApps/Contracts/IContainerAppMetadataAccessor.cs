using Proto.Cluster.AzureContainerApps.Models;

namespace Proto.Cluster.AzureContainerApps.Contracts;

/// <summary>
/// Provides access to the container app metadata.
/// </summary>
public interface IContainerAppMetadataAccessor
{
    /// <summary>
    /// Gets the container app metadata.
    /// </summary>
    ValueTask<ContainerAppMetadata> GetMetadataAsync(CancellationToken cancellationToken = default);
}