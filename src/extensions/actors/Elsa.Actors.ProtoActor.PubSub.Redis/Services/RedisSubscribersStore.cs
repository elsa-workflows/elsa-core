using Elsa.Actors.ProtoActor.PubSub.Redis.Options;
using Google.Protobuf;
using Proto.Cluster.PubSub;
using Proto.Utils;
using StackExchange.Redis;

namespace Elsa.Actors.ProtoActor.PubSub.Redis.Services;

/// <summary>
/// Stores Proto.Actor Pub/Sub subscribers in Redis.
/// </summary>
public sealed class RedisSubscribersStore(
    string clusterName,
    IDatabase database,
    RedisSubscribersStoreOptions? options = null) : IKeyValueStore<Subscribers>
{
    private readonly string _keyPrefix = options?.KeyPrefix ?? "proto";
    private readonly Func<string, string, RedisKey>? _keyFormatter = options?.KeyFormatter;

    /// <inheritdoc />
    public async Task<Subscribers> GetAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var value = await database.StringGetAsync(GetKey(id));
        return value.IsNullOrEmpty ? new Subscribers() : Subscribers.Parser.ParseFrom(value);
    }

    /// <inheritdoc />
    public Task SetAsync(string id, Subscribers state, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return database.StringSetAsync(GetKey(id), state.ToByteArray());
    }

    /// <inheritdoc />
    public Task ClearAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return database.KeyDeleteAsync(GetKey(id));
    }

    private RedisKey GetKey(string id) =>
        _keyFormatter?.Invoke(clusterName, id) ?? $"{_keyPrefix}:{clusterName}:pubsub:topic:{id}";
}