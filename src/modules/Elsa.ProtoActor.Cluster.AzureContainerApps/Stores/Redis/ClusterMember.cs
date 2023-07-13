namespace Proto.Cluster.AzureContainerApps.Stores.Redis;

/// <summary>
/// A member with a cluster name and kind values.
/// </summary>
public record ClusterMember(string Host, int Port, string ClusterName, IEnumerable<string> Kinds);