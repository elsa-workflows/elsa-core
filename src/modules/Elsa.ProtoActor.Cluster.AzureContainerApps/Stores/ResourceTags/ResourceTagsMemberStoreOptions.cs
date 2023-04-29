namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

public class ResourceTagsMemberStoreOptions
{
    public string? SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; } = default!;
    public string ResourceName { get; set; } = default!;
    // [CanBeNull] public string ContainerAppName { get; set; } = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME");
    // [CanBeNull] public string RevisionName { get; set; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION");
    // [CanBeNull] public string ReplicaName { get; set; } = Environment.GetEnvironmentVariable("HOSTNAME");
}