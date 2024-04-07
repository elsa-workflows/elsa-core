using Elsa.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Proto.Cluster;

namespace Elsa.ProtoActor.Services;

public class ProtoActorWorkflowClient(Cluster cluster) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowInstanceId { get; set; }

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteAndWaitAsync(IExecuteWorkflowRequest? @params = null, CancellationToken cancellationToken = default)
    {
        var grain = cluster.GetNamedWorkflowGrain(WorkflowInstanceId);
        var request = new ProtoExecuteWorkflowRequest
        {
            
        };
        //var response = await client.Start(request, @params?.CancellationToken ?? default);

        throw new NotImplementedException();
    }

    public async Task ExecuteAndForgetAsync(IExecuteWorkflowRequest? @params = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<CancellationResult> CancelAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private ProtoExecuteWorkflowRequest Map(IExecuteWorkflowRequest request)
    {
        return new()
        {
            DefinitionVersionId = request.DefinitionVersionId,
            ActivityHandle = Map(request.ActivityHandle),
            BookmarkId = request.BookmarkId,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            TriggerActivityId = request.TriggerActivityId,
            Input = request.Input?.SerializeInput(),
            CorrelationId = request.CorrelationId,
            Properties = request.Properties?.SerializeProperties(),
        };
    }
    
    private ProtoActivityHandle Map(ActivityHandle source)
    {
        return new()
        {
            ActivityId = source.ActivityId,
            ActivityHash = source.ActivityHash,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityNodeId = source.ActivityNodeId,
        };
    }
}