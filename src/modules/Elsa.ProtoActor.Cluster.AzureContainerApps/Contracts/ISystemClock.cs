namespace Proto.Cluster.AzureContainerApps.Contracts;

/// <summary>
/// Provides the current time in UTC.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// The current time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}