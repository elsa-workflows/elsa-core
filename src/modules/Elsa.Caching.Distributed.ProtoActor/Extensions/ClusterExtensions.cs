using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    private static readonly string ActorId = Guid.NewGuid().ToString();
    
    public static LocalCacheClient GetLocalCacheClient(this Cluster cluster)
    {
        return cluster.GetLocalCache($"{nameof(LocalCacheActor)}-{ActorId}");
    }
}