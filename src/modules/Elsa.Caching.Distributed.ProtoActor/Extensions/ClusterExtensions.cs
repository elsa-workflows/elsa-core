using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    private static readonly string ActorId = Guid.NewGuid().ToString();
    
    public static LocalCacheClient GetNamedLocalCacheClient(this Cluster cluster, string workflowInstanceId)
    {
        return cluster.GetLocalCache($"{nameof(LocalCacheActor)}-{ActorId}");
    }
}