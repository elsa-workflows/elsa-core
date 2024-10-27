using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.State;
using Google.Protobuf;
using JetBrains.Annotations;
using Proto;
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
    public ProtoActorWorkflowClient(string workflowInstanceId, Cluster cluster, Mappers.Mappers mappers, ITenantAccessor tenantAccessor)
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
        var response = await Create(protoRequest, cancellationToken);
        return _mappers.CreateWorkflowInstanceResponseMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.RunWorkflowInstanceRequestMapper.Map(request);
        var response = await Run(protoRequest, cancellationToken);
        return _mappers.RunWorkflowInstanceResponseMapper.Map(WorkflowInstanceId, response!);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var protoRequest = _mappers.CreateAndRunWorkflowInstanceRequestMapper.Map(WorkflowInstanceId, request);
        var response = await CreateAndRun(protoRequest, cancellationToken);
        return _mappers.RunWorkflowInstanceResponseMapper.Map(WorkflowInstanceId, response!);
    }
   

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await Cancel(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await ExportState(cancellationToken);
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
    
    // Copied and adapted from generated grains in order to allow for custom headers.
    private async Task<Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateWorkflowInstanceResponse?> Create(Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        return await RequestAsync<Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateWorkflowInstanceRequest, Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateWorkflowInstanceResponse>(request, 0, cancellationToken);
    }
    
    private async Task<Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse?> Run(Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        return await RequestAsync<Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceRequest, Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse>(request, 1, cancellationToken);
    }
    
    private async Task<Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse?> CreateAndRun(Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        return await RequestAsync<Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateAndRunWorkflowInstanceRequest, Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse>(request, 2, cancellationToken);
    }
    
    public async Task Cancel(CancellationToken cancellationToken)
    {
        await RequestAsync<Google.Protobuf.WellKnownTypes.Empty, Google.Protobuf.WellKnownTypes.Empty>(null, 4, cancellationToken);
    }
    
    public async Task<ExportWorkflowStateResponse?> ExportState(CancellationToken cancellationToken)
    {
        return await RequestAsync<Google.Protobuf.WellKnownTypes.Empty, ExportWorkflowStateResponse>(null, 5, cancellationToken);
    }
    
    private string GrainIdentity => $"WorkflowInstanceActor-{WorkflowInstanceId}";

    private MessageEnvelope CreateEnvelope(GrainRequestMessage gr)
    {
        var headers = new Dictionary<string, string>();
        if(_tenantAccessor.Tenant != null) headers["TenantId"] = _tenantAccessor.Tenant.Id;
        return new MessageEnvelope(gr, _cluster.System.Root.Self, new MessageHeader(headers));
    }
    
    private async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest? request, int methodIndex, CancellationToken cancellationToken) where TRequest : IMessage
    {
        var gr = new GrainRequestMessage(methodIndex, request);
        var envelope = CreateEnvelope(gr);
        var res = await _cluster.RequestAsync<object>(GrainIdentity, WorkflowInstanceActor.Kind, envelope, cancellationToken);

        return res switch
        {
            TResponse message => message,
            GrainResponseMessage grainResponse => (TResponse?)grainResponse.ResponseMessage,
            GrainErrorResponse grainErrorResponse => throw new Exception(grainErrorResponse.Err),
            null => default,
            _ => throw new NotSupportedException($"Unknown response type {res.GetType().FullName}")
        };
    }
}