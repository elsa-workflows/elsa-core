namespace Proto.Cluster.AzureContainerApps.Models;

/// <summary>
/// Contains information about the container app name, revision name and replica name.
/// </summary>
/// <param name="ContainerAppName">The name of the container app.</param>
/// <param name="RevisionName">The name of the revision.</param>
/// <param name="ReplicaName">The name of the replica.</param>
public record ContainerAppMetadata(string ContainerAppName, string RevisionName, string ReplicaName);