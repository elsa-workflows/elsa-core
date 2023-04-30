namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Options for the <see cref="ResourceTagsClusterMemberStore"/>.
/// </summary>
public class ResourceTagsMemberStoreOptions
{
    /// <summary>
    /// The subscription ID to use. If not set, the default subscription will be used.
    /// </summary>
    public string? SubscriptionId { get; set; }
    
    /// <summary>
    /// The name of the resource group to use.
    /// </summary>
    public string ResourceGroupName { get; set; } = default!;
}