using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.ProtoBuf;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    public static WorkflowInstanceClient GetNamedWorkflowGrain(this Cluster cluster, string workflowInstanceId) => cluster.GetWorkflowInstance($"{nameof(WorkflowInstance)}-{workflowInstanceId}");
}