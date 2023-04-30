namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Options for the <see cref="ResourceTagsMemberStore"/>.
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
    
    /// <summary>
    /// The name of the resource to use for storing tags.
    /// </summary>
    public string ResourceName { get; set; } = default!;
}