using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    private const string WorkflowInstanceActorPrefix = nameof(WorkflowInstanceActor) + "-";

    public static WorkflowInstanceClient GetNamedWorkflowInstanceClient(this Cluster cluster, string workflowInstanceId)
    {
        return cluster.GetWorkflowInstance(WorkflowInstanceActorPrefix + workflowInstanceId);
    }

    public static string GetWorkflowInstanceId(string actorId)
    {
        return actorId.StartsWith(WorkflowInstanceActorPrefix, StringComparison.Ordinal)
            ? actorId[WorkflowInstanceActorPrefix.Length..]
            : actorId;
    }
}
