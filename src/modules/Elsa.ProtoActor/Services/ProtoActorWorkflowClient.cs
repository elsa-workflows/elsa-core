using Elsa.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Proto.Cluster;

namespace Elsa.ProtoActor.Services;

/// <summary>
/// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
/// </summary>
public class ProtoActorWorkflowClient : IWorkflowClient
{
    private readonly Cluster _cluster;
    private readonly Mappers.Mappers _mappers;
    private WorkflowClient _grain = default!;

    /// <summary>
    /// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
    /// </summary>
    public ProtoActorWorkflowClient(Cluster cluster, Mappers.Mappers mappers)
    {
        _cluster = cluster;
        _mappers = mappers;
    }

    private string WorkflowDefinitionVersionId { get; set; } = default!;
    private string WorkflowInstanceId { get; set; } = default!;

    /// <inheritdoc />
    public ValueTask InitializeAsync(string workflowDefinitionVersionId, string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        WorkflowDefinitionVersionId = workflowDefinitionVersionId;
        WorkflowInstanceId = workflowInstanceId;
        _grain = _cluster.GetNamedWorkflowGrain(WorkflowInstanceId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteAndWaitAsync(IExecuteWorkflowRequest? request = null, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.ExecuteWorkflowRequestMapper.Map(WorkflowDefinitionVersionId, WorkflowInstanceId, request);
        var response = await _grain.ExecuteAndWait(protoRequest, cancellationToken);
        return Map(response!);
    }

    /// <inheritdoc />
    public async Task ExecuteAndForgetAsync(IExecuteWorkflowRequest? request = default, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.ExecuteWorkflowRequestMapper.Map(WorkflowDefinitionVersionId, WorkflowInstanceId, request);
        await _grain.ExecuteAndForget(protoRequest, cancellationToken);
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
        return await _mappers.WorkflowStateJsonMapper.MapAsync(response!.SerializedWorkflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var protoJson = await _mappers.WorkflowStateJsonMapper.MapAsync(workflowState, cancellationToken);
        var request = new ProtoImportWorkflowStateRequest
        {
            SerializedWorkflowState = protoJson
        };
        await _grain.ImportState(request, cancellationToken);
    }

    private ExecuteWorkflowResult Map(ProtoExecuteWorkflowResponse source)
    {
        return new ExecuteWorkflowResult(
            source.WorkflowInstanceId,
            _mappers.BookmarkDiffMapper.Map(source.BookmarkDiff),
            _mappers.WorkflowStatusMapper.Map(source.Status),
            _mappers.WorkflowSubStatusMapper.Map(source.SubStatus),
            _mappers.ActivityIncidentMapper.Map(source.Incidents).ToList()
        );
    }
}