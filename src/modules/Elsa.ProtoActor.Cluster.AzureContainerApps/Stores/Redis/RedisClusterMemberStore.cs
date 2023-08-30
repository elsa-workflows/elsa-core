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
    private readonly IRedisConnectionMultiplexerProvider _connectionMultiplexerProvider;
    private readonly ISystemClock _systemClock;
    private const string ClusterKey = "proto:cluster:members";
    private IDatabase? _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisClusterMemberStore"/> class.
    /// </summary>
    public RedisClusterMemberStore(IRedisConnectionMultiplexerProvider connectionMultiplexerProvider, ISystemClock systemClock)
    {
        _connectionMultiplexerProvider = connectionMultiplexerProvider;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async ValueTask<ICollection<StoredMember>> ListAsync(CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        var entries = await database.HashGetAllAsync(ClusterKey);
        return entries.Select(entry => Deserialize(entry.Value!)).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<StoredMember> RegisterAsync(string clusterName, Member member, CancellationToken cancellationToken = default)
    {
        var storedMember = StoredMember.FromMember(member, clusterName, _systemClock.UtcNow);
        var serialized = Serialize(storedMember);
        var database = await GetDatabaseAsync();
        
        await database.HashSetAsync(ClusterKey, member.Id, serialized);
        return storedMember;
    }

    /// <inheritdoc />
    public async ValueTask UnregisterAsync(string memberId, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        await database.HashDeleteAsync(ClusterKey, memberId);
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(StoredMember storedMember, CancellationToken cancellationToken = default)
    {
        var serialized = Serialize(storedMember);
        var database = await GetDatabaseAsync();
        await database.HashSetAsync(ClusterKey, storedMember.Id, serialized);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        await database.KeyDeleteAsync(ClusterKey);
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        if(_database is not null)
            return _database;
        
        var connectionMultiplexer = await _connectionMultiplexerProvider.GetConnectionMultiplexerAsync();
        return _database = connectionMultiplexer.GetDatabase();
    }

    private static string Serialize(StoredMember member) => JsonSerializer.Serialize(member);
    private static StoredMember Deserialize(string data) => JsonSerializer.Deserialize<StoredMember>(data)!;
}