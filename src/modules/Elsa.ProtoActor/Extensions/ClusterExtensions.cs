using Elsa.ProtoActor.Actors;
using Elsa.ProtoActor.ProtoBuf;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    public static WorkflowInstanceClient GetNamedWorkflowGrain(this Cluster cluster, string workflowInstanceId)
    {
        return cluster.GetWorkflowInstance($"{nameof(WorkflowInstanceActor)}-{workflowInstanceId}");
    }
}