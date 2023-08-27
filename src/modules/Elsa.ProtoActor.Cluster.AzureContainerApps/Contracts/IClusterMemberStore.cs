using Proto.Cluster.AzureContainerApps.Models;

namespace Proto.Cluster.AzureContainerApps.Contracts;

/// <summary>
/// Represents a repository of members in a cluster.
/// </summary>
public interface IClusterMemberStore
{
    /// <summary>
    /// Returns a list of all members.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all members in the cluster.</returns>
    ValueTask<ICollection<StoredMember>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a member.
    /// </summary>
    /// <param name="clusterName">The name of the cluster.</param>
    /// <param name="member">The member to register.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<StoredMember> RegisterAsync(string clusterName, Member member, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a member from the cluster.
    /// </summary>
    /// <param name="memberId">The ID of the member to unregister.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a member.
    /// </summary>
    /// <param name="storedMember">The member to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask UpdateAsync(StoredMember storedMember, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all members.
    /// </summary>
    /// <param name="clusterName">The name of the cluster.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ClearAsync(string clusterName, CancellationToken cancellationToken = default);
}