using Elsa.Actors.ProtoActor.Features;
using Elsa.Actors.ProtoActor.PubSub.Redis.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for configuring Redis-backed Proto.Actor Pub/Sub subscriber storage.
/// </summary>
[PublicAPI]
public static class RedisProtoActorFeatureExtensions
{
    /// <summary>
    /// Configures the core Proto.Actor feature to use Redis-backed Pub/Sub subscriber storage.
    /// </summary>
    /// <param name="feature">The core Proto.Actor feature to configure.</param>
    /// <param name="configure">An optional delegate that configures the Redis feature.</param>
    /// <returns>The core Proto.Actor feature.</returns>
    public static ProtoActorFeature UseRedisPubSubSubscribersStore(
        this ProtoActorFeature feature,
        Action<RedisPubSubSubscribersStoreFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}