using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.Protos;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    public static RunningWorkflowsGrainClient GetNamedRunningWorkflowsGrain(this Cluster cluster) => cluster.GetRunningWorkflowsGrain(nameof(RunningWorkflowsGrain));
    public static WorkflowGrainClient GetNamedWorkflowGrain(this Cluster cluster, string workflowInstanceId) => cluster.GetWorkflowGrain($"{nameof(WorkflowGrain)}-{workflowInstanceId}");
}