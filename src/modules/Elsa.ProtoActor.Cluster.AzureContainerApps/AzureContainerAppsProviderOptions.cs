using JetBrains.Annotations;

namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// Options for <see cref="AzureContainerAppsProvider"/>
/// </summary>
[PublicAPI]
public class AzureContainerAppsProviderOptions
{
    /// <summary>
    /// The subscription ID to use. If not set, the default subscription will be used.
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// The name of the resource group to use.
    /// </summary>
    public string ResourceGroupName { get; set; } = default!;

    /// <summary>
    /// The name of the container app to use.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
}