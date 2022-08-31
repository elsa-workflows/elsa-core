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
    
    public WorkflowGrainClient CreateWorkflowGrainClient(string workflowInstanceId) => _cluster.GetWorkflowGrain(workflowInstanceId);
    public BookmarkGrainClient CreateBookmarkGrainClient(string hash) => _cluster.GetBookmarkGrain(hash);
}