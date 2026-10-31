using StackExchange.Redis;

namespace Elsa.Actors.ProtoActor.PubSub.Redis.Options;

/// <summary>
/// Configures how Proto.Actor Pub/Sub subscriber keys are stored in Redis.
/// </summary>
public sealed class RedisSubscribersStoreOptions
{
    /// <summary>
    /// Gets or sets the prefix used by the default key format.
    /// </summary>
    /// <remarks>
    /// The default format is <c>{KeyPrefix}:{clusterName}:pubsub:topic:{topic}</c>.
    /// This property is ignored when <see cref="KeyFormatter"/> is configured.
    /// </remarks>
    public string KeyPrefix { get; set; } = "proto";

    /// <summary>
    /// Gets or sets an optional delegate that creates the complete Redis key from the cluster name and topic.
    /// </summary>
    public Func<string, string, RedisKey>? KeyFormatter { get; set; }
}