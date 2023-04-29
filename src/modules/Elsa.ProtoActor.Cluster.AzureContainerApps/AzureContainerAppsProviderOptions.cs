using JetBrains.Annotations;

namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// Options for <see cref="AzureContainerAppsProvider"/>
/// </summary>
public class AzureContainerAppsProviderOptions
{
    /// <summary>
    /// The subscription ID to use. If not set, the default subscription will be used.
    /// </summary>
    public string? SubscriptionId { get; set; } = default!;
    
    /// <summary>
    /// The name of the resource group to use.
    /// </summary>
    public string ResourceGroupName { get; set; } = default!;
    
    /// <summary>
    /// The name of the container app to use.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// The name of the container app to use.
    /// </summary>
    [CanBeNull] public string ContainerAppName { get; set; } = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME");
    
    /// <summary>
    /// The name of the revision to use.
    /// </summary>
    [CanBeNull] public string RevisionName { get; set; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION");
    
    /// <summary>
    /// The name of the replica to use.
    /// </summary>
    [CanBeNull] public string ReplicaName { get; set; } = Environment.GetEnvironmentVariable("HOSTNAME");
    
    /// <summary>
    /// The advertised host to use.
    /// </summary>
    [CanBeNull] public string AdvertisedHost { get; set; } = ConfigUtils.FindSmallestIpAddress().ToString();
}