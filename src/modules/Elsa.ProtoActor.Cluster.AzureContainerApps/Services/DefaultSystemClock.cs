using Proto.Cluster.AzureContainerApps.Contracts;

namespace Proto.Cluster.AzureContainerApps.Services;

/// <inheritdoc />
public class DefaultSystemClock : ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}