namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// A member with a cluster name.
/// </summary>
public record TaggedMember(string Host, int Port, string Cluster);