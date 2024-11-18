using Elsa.Caching.Distributed.Features;
using Elsa.Caching.Distributed.ProtoActor.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods on <see cref="DistributedCacheFeature"/>.
/// </summary>
[PublicAPI]
public static class ProtoActorDistributedCacheFeatureExtensions
{
    /// <summary>
    /// Configure the distributed cache feature using Proto.Actor.
    /// </summary>
    public static DistributedCacheFeature UseProtoActor(this DistributedCacheFeature feature, Action<ProtoActorDistributedCacheFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}