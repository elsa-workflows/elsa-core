using System.Text.Json.Serialization;
using Proto.Cluster.AzureContainerApps.Contracts;

namespace Proto.Cluster.AzureContainerApps.Models;

/// <summary>
/// Represents a member in a cluster stored in a <see cref="IClusterMemberStore"/> in a storable format.
/// </summary>
public record StoredMember(string Id, string Host, int Port, ICollection<string> Kinds, string Cluster, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Gets the address of the member.
    /// </summary>
    [JsonIgnore]
    public string Address => Host + ":" + Port;

    /// <summary>
    /// Creates a new instance of the <see cref="Member"/> class from the values in this instance.
    /// </summary>
    /// <returns></returns>
    public Member ToMember() => new()
    {
        Id = Id,
        Host = Host,
        Port = Port,
        Kinds = { Kinds }
    };

    /// <summary>
    /// Creates a new instance of the <see cref="StoredMember"/> class from the values in the specified <see cref="Member"/> instance.
    /// </summary>
    /// <param name="member">The member to create a new instance from.</param>
    /// <param name="cluster">The name of the cluster.</param>
    /// <param name="createdAt">The time the member was created.</param>
    /// <returns>A new instance of the <see cref="StoredMember"/> class.</returns>
    public static StoredMember FromMember(Member member, string cluster, DateTimeOffset createdAt)
    {
        return new StoredMember(member.Id, member.Host, member.Port, member.Kinds, cluster, createdAt, createdAt);
    }
}