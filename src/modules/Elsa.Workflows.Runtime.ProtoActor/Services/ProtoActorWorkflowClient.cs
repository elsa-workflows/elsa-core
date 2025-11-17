using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.ProtoActor;
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
    private readonly Cluster _cluster;
    private readonly Mappers.Mappers _mappers;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly WorkflowInstanceClient _actorClient;

    /// <summary>
    /// A workflow client that uses Proto.Actor to communicate with the workflow running in the cluster.
    /// </summary>
    public ProtoActorWorkflowClient(string workflowInstanceId, Cluster cluster, Mappers.Mappers mappers, ITenantAccessor tenantAccessor, IWorkflowActivationStrategyEvaluator workflowActivationStrategyEvaluator)
    {
        WorkflowInstanceId = workflowInstanceId;
        _cluster = cluster;
        _mappers = mappers;
        _tenantAccessor = tenantAccessor;
        _actorClient = cluster.GetNamedWorkflowInstanceClient(WorkflowInstanceId);
    }

    /// <inheritdoc />
    public string WorkflowInstanceId { get; }

    /// <inheritdoc />
    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await ExecuteAsync(client => client.Create(protoRequest, CreateHeaders(), cancellationToken));
        return _mappers.CreateWorkflowInstanceResponseMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.RunWorkflowInstanceRequestMapper.Map(request);
        var response = await ExecuteAsync(client => client.Run(protoRequest, CreateHeaders(), cancellationToken));
        return _mappers.RunWorkflowInstanceResponseMapper.Map(WorkflowInstanceId, response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateAndRunWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await ExecuteAsync(client => client.CreateAndRun(protoRequest, CreateHeaders(), cancellationToken));
        return _mappers.RunWorkflowInstanceResponseMapper.Map(WorkflowInstanceId, response!);
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(client => client.Cancel(CreateHeaders(), cancellationToken));
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(client => client.ExportState(CreateHeaders(), cancellationToken));
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
        await ExecuteAsync(client => client.ImportState(request, CreateHeaders(), cancellationToken));
    }

    public Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task ExecuteAsync(Func<WorkflowInstanceClient, Task> action)
    {
        await _cluster.JoinedCluster;
        await action(_actorClient);
    }

    private async Task<T> ExecuteAsync<T>(Func<WorkflowInstanceClient, Task<T>> action)
    {
        await _cluster.JoinedCluster;
        return await action(_actorClient);
    }

    private IDictionary<string, string> CreateHeaders()
    {
        var headers = new Dictionary<string, string>();
        if (_tenantAccessor.Tenant != null) headers[HeaderNames.TenantId] = _tenantAccessor.Tenant.Id;
        return headers;
    }
}