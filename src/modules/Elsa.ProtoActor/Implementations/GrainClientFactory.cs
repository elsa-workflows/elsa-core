using Elsa.Persistence.Common.Models;
using Elsa.Runtime.Protos;
using Proto.Cluster;

namespace Elsa.ProtoActor.Implementations;

public class GrainClientFactory
{
    private readonly Cluster _cluster;

    public GrainClientFactory(Cluster cluster)
    {
        _cluster = cluster;
    }

    public WorkflowDefinitionGrainClient CreateWorkflowDefinitionGrainClient(string workflowDefinitionId, VersionOptions versionOptions) => _cluster.GetWorkflowDefinitionGrain($"workflow-definition:{workflowDefinitionId}:{versionOptions.ToString()}");
    public WorkflowInstanceGrainClient CreateWorkflowInstanceGrainClient(string workflowInstanceId) => _cluster.GetWorkflowInstanceGrain($"workflow-instance:{workflowInstanceId}");
    public WorkflowGrainClient CreateWorkflowGrainClient(string workflowInstanceId) => _cluster.GetWorkflowGrain($"workflow:{workflowInstanceId}");
}