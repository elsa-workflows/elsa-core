using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.Protos;
using Proto.Cluster;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ClusterExtensions
{
    public static RunningWorkflowsGrainClient GetRunningWorkflowsGrain(this Cluster cluster) => cluster.GetRunningWorkflowsGrain(nameof(RunningWorkflowsGrain));
}