namespace Proto.Cluster.AzureContainerApps.Messages;

/// <summary>
/// Registers a member in the cluster.
/// </summary>
public record RegisterMember(string ClusterName, string Host, int Port, ICollection<string> Kinds, string MemberId);