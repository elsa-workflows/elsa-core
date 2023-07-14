using System.Text.Json;
using JetBrains.Annotations;
using StackExchange.Redis;

namespace Proto.Cluster.AzureContainerApps.Stores.Redis;

/// <summary>
/// Stores cluster member information in Redis database.
/// </summary>
[PublicAPI]
public class RedisClusterMemberStore : IClusterMemberStore
{
    private const string ClusterKey = "proto:cluster:members";
    private readonly IDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisClusterMemberStore"/> class.
    /// </summary>
    public RedisClusterMemberStore(ConnectionMultiplexer connectionMultiplexer) => _database = connectionMultiplexer.GetDatabase();

    /// <inheritdoc />
    public async ValueTask<ICollection<Member>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _database.HashGetAllAsync(ClusterKey);

        return entries.Select(entry =>
        {
            var clusterMember = Deserialize(entry.Value!);
            return new Member
            {
                Id = entry.Name,
                Host = clusterMember.Host,
                Port = clusterMember.Port,
                Kinds = { clusterMember.Kinds }
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async ValueTask RegisterAsync(string clusterName, Member member, CancellationToken cancellationToken = default)
    {
        var clusterMember = new ClusterMember(member.Host, member.Port, clusterName, member.Kinds);
        var serialized = Serialize(clusterMember);

        await _database.HashSetAsync(ClusterKey, member.Id, serialized);
    }

    /// <inheritdoc />
    public async ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default)
    {
        await _database.HashDeleteAsync(ClusterKey, memberId);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(ClusterKey);
    }

    private static string Serialize(ClusterMember member) => JsonSerializer.Serialize(member);
    private static ClusterMember Deserialize(string data) => JsonSerializer.Deserialize<ClusterMember>(data)!;
}