using System.Text.Json;
using JetBrains.Annotations;
using Proto.Cluster.AzureContainerApps.Contracts;
using Proto.Cluster.AzureContainerApps.Models;
using StackExchange.Redis;

namespace Proto.Cluster.AzureContainerApps.Stores.Redis;

/// <summary>
/// Stores cluster member information in Redis database.
/// </summary>
[PublicAPI]
public class RedisClusterMemberStore : IClusterMemberStore
{
    private readonly ISystemClock _systemClock;
    private const string ClusterKey = "proto:cluster:members";
    private readonly IDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisClusterMemberStore"/> class.
    /// </summary>
    public RedisClusterMemberStore(ConnectionMultiplexer connectionMultiplexer, ISystemClock systemClock)
    {
        _systemClock = systemClock;
        _database = connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc />
    public async ValueTask<ICollection<StoredMember>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _database.HashGetAllAsync(ClusterKey);
        return entries.Select(entry => Deserialize(entry.Value!)).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<StoredMember> RegisterAsync(string clusterName, Member member, CancellationToken cancellationToken = default)
    {
        var storedMember = StoredMember.FromMember(member, clusterName, _systemClock.UtcNow);
        var serialized = Serialize(storedMember);

        await _database.HashSetAsync(ClusterKey, member.Id, serialized);
        return storedMember;
    }

    /// <inheritdoc />
    public async ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default)
    {
        await _database.HashDeleteAsync(ClusterKey, memberId);
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(StoredMember storedMember, CancellationToken cancellationToken = default)
    {
        var serialized = Serialize(storedMember);

        await _database.HashSetAsync(ClusterKey, storedMember.Id, serialized);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(ClusterKey);
    }

    private static string Serialize(StoredMember member) => JsonSerializer.Serialize(member);
    private static StoredMember Deserialize(string data) => JsonSerializer.Deserialize<StoredMember>(data)!;
}