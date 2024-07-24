using Elsa.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Proto.Cluster;
using CreateAndRunWorkflowInstanceRequest = Elsa.Workflows.Runtime.Messages.CreateAndRunWorkflowInstanceRequest;
using CreateWorkflowInstanceRequest = Elsa.Workflows.Runtime.Messages.CreateWorkflowInstanceRequest;
using CreateWorkflowInstanceResponse = Elsa.Workflows.Runtime.Messages.CreateWorkflowInstanceResponse;
using RunWorkflowInstanceRequest = Elsa.Workflows.Runtime.Messages.RunWorkflowInstanceRequest;
using RunWorkflowInstanceResponse = Elsa.Workflows.Runtime.Messages.RunWorkflowInstanceResponse;

namespace Elsa.Workflows.Runtime.ProtoActor.Services;

/// <summary>
/// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
/// </summary>
[UsedImplicitly]
public class ProtoActorWorkflowClient : IWorkflowClient
{
    private readonly Mappers.Mappers _mappers;
    private readonly WorkflowInstanceClient _actorClient;

    /// <summary>
    /// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
    /// </summary>
    public ProtoActorWorkflowClient(string workflowInstanceId, Cluster cluster, Mappers.Mappers mappers)
    {
        WorkflowInstanceId = workflowInstanceId;
        _mappers = mappers;
        _actorClient = cluster.GetNamedWorkflowInstanceClient(WorkflowInstanceId);
    }

    /// <inheritdoc />
    public string WorkflowInstanceId { get; }

    /// <inheritdoc />
    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await _actorClient.Create(protoRequest, cancellationToken);
        return _mappers.CreateWorkflowInstanceResponseMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.RunWorkflowInstanceRequestMapper.Map(request);
        var response = await _actorClient.Run(protoRequest, cancellationToken);
        return _mappers.RunWorkflowInstanceResponseMapper.Map(WorkflowInstanceId, response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateAndRunWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await _actorClient.CreateAndRun(protoRequest, cancellationToken);
        return _mappers.RunWorkflowInstanceResponseMapper.Map(WorkflowInstanceId, response!);
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await _actorClient.Cancel(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _actorClient.ExportState(cancellationToken);
        return _mappers.WorkflowStateJsonMapper.Map(response!.SerializedWorkflowState);
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var protoJson = _mappers.WorkflowStateJsonMapper.Map(workflowState);
        var request = new ImportWorkflowStateRequest
        {
            SerializedWorkflowState = protoJson
        };
        await _actorClient.ImportState(request, cancellationToken);
    }
}