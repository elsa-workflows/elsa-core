using Elsa.Caching.Distributed.Features;
using Elsa.Caching.Distributed.ProtoActor.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Provides extension methods on <see cref="DistributedCacheFeature"/>.
[PublicAPI]
public static class ProtoActorDistributedCacheFeatureExtensions
{
    /// Configure the distributed cache feature using Proto.Actor.
    public static DistributedCacheFeature UseProtoActor(this DistributedCacheFeature feature, Action<ProtoActorDistributedCacheFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}