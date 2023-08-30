using JetBrains.Annotations;
using Proto.Cluster.AzureContainerApps.ClusterProviders;
using Proto.Cluster.AzureContainerApps.Contracts;

namespace Proto.Cluster.AzureContainerApps.Options;

/// <summary>
/// Options for <see cref="AzureContainerAppsProvider"/>
/// </summary>
[PublicAPI]
public class AzureContainerAppsProviderOptions
{
    /// <summary>
    /// The subscription ID to use. If not set, the default subscription will be used.
    /// </summary>
    [CanBeNull]
    public string SubscriptionId { get; set; }

    /// <summary>
    /// The name of the resource group to use.
    /// </summary>
    public string ResourceGroupName { get; set; } = default!;

    /// <summary>
    /// The interval at which to poll the cluster member store for changes.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// The time to live for a member in the store before it is considered stale and removed from the underlying <see cref="IClusterMemberStore"/>.
    /// The actual TTL is determined by the <see cref="PollInterval"/> and the <see cref="MemberTimeToLive"/> by adding them together.
    /// </summary>
    public TimeSpan MemberTimeToLive { get; set; } = TimeSpan.FromMinutes(1);
}