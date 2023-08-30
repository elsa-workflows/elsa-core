using Proto.Cluster.AzureContainerApps.Contracts;
using Proto.Cluster.AzureContainerApps.Models;

namespace Proto.Cluster.AzureContainerApps.Services;

/// <summary>
/// 
/// </summary>
public class EnvironmentContainerAppMetadataAccessor : IContainerAppMetadataAccessor
{
    /// <summary>
    /// Returns the container app metadata from the environment variables.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="ContainerAppMetadata"/> instance.</returns>
    /// <exception cref="Exception">Thrown if any of the required environment variables are not set.</exception>
    public ValueTask<ContainerAppMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var containerAppName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ?? throw new Exception("No app name provided");
        var revisionName = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? throw new Exception("No app revision provided");
        var replicaName = Environment.GetEnvironmentVariable("HOSTNAME") ?? throw new Exception("No replica name provided");
        
        return new(new ContainerAppMetadata(containerAppName, revisionName, replicaName));
    }
}