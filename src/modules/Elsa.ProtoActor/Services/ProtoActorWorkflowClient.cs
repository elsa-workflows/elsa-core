using Elsa.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Proto.Cluster;

namespace Elsa.ProtoActor.Services;

/// <summary>
/// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
/// </summary>
[UsedImplicitly]
public class ProtoActorWorkflowClient : IWorkflowClient
{
    private readonly Mappers.Mappers _mappers;
    private readonly WorkflowInstanceClient _grain;

    /// <summary>
    /// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
    /// </summary>
    public ProtoActorWorkflowClient(string workflowInstanceId, Cluster cluster, Mappers.Mappers mappers)
    {
        WorkflowInstanceId = workflowInstanceId;
        _mappers = mappers;
        _grain = cluster.GetNamedWorkflowGrain(WorkflowInstanceId);
    }

    /// <inheritdoc />
    public string WorkflowInstanceId { get; }

    /// <inheritdoc />
    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await _grain.Create(protoRequest, cancellationToken);
        return _mappers.CreateWorkflowInstanceResponseMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.RunWorkflowInstanceRequestMapper.Map(request);
        var response = await _grain.Run(protoRequest, cancellationToken);
        return _mappers.RunWorkflowInstanceResponseMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateAndRunWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await _grain.CreateAndRun(protoRequest, cancellationToken);
        return _mappers.RunWorkflowInstanceResponseMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await _grain.Cancel(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _grain.ExportState(cancellationToken);
        return _mappers.WorkflowStateJsonMapper.Map(response!.SerializedWorkflowState);
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var protoJson = _mappers.WorkflowStateJsonMapper.Map(workflowState);
        var request = new ProtoImportWorkflowStateRequest
        {
            SerializedWorkflowState = protoJson
        };
        await _grain.ImportState(request, cancellationToken);
    }
}