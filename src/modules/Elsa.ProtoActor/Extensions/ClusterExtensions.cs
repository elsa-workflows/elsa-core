using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.ProtoBuf;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    public static WorkflowClient GetNamedWorkflowGrain(this Cluster cluster, string workflowInstanceId)
    {
        return cluster.GetWorkflow($"{nameof(WorkflowGrain)}-{workflowInstanceId}");
    }
}