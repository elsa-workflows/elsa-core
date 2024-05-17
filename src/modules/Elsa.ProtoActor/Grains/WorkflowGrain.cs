using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.ProtoActor.Snapshots;
using Elsa.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using WorkflowBase = Elsa.ProtoActor.ProtoBuf.WorkflowBase;

namespace Elsa.ProtoActor.Grains;

internal class WorkflowGrain : WorkflowBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly Mappers.Mappers _mappers;
    private readonly Persistence _persistence;
    private string? _workflowInstanceId;
    private IWorkflowHost? _workflowHost;

    /// <inheritdoc />
    public WorkflowGrain(
        IContext context,
        IServiceScopeFactory scopeFactory,
        IWorkflowHostFactory workflowHostFactory,
        IProvider provider,
        Mappers.Mappers mappers) : base(context)
    {
        _scopeFactory = scopeFactory;
        _workflowHostFactory = workflowHostFactory;
        _mappers = mappers;
        _persistence = Persistence.WithSnapshotting(provider, context.ClusterIdentity()!.Identity, ApplySnapshot);
    }

    public override async Task OnStarted()
    {
        await _persistence.RecoverStateAsync();
    }

    public override async Task OnStopped()
    {
        await SaveSnapshotAsync();
    }

    public override Task<ProtoCreateWorkflowInstanceResponse> Create(ProtoCreateWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task Create(ProtoCreateWorkflowInstanceRequest request, Action<ProtoCreateWorkflowInstanceResponse> respond, Action<string> onError)
    {
        Context.ReenterAfter(CreateAsync(request), result =>
        {
            if (result.IsFaulted)
                onError(result.Exception.Message);
            else
                respond(new ProtoCreateWorkflowInstanceResponse());
        });

        return Task.CompletedTask;
    }

    public override Task<ProtoRunWorkflowInstanceResponse> Run(ProtoRunWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task Run(ProtoRunWorkflowInstanceRequest request, Action<ProtoRunWorkflowInstanceResponse> respond, Action<string> onError)
    {
        var mappedRequest = _mappers.RunWorkflowParamsMapper.Map(request);
        
        Context.ReenterAfter(RunAsync(mappedRequest), async completedTask =>
        {
            if (completedTask.IsFaulted)
                onError(completedTask.Exception.Message);
            else
            {
                var result = await completedTask;
                respond(_mappers.RunWorkflowInstanceResponseMapper.Map(result));
            }
        });

        return Task.CompletedTask;
    }

    public override Task<ProtoRunWorkflowInstanceResponse> CreateAndRun(ProtoCreateAndRunWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task CreateAndRun(ProtoCreateAndRunWorkflowInstanceRequest request, Action<ProtoRunWorkflowInstanceResponse> respond, Action<string> onError)
    {
        Context.ReenterAfter(CreateAndRunAsync(request), async completedTask =>
        {
            if (completedTask.IsFaulted)
                onError(completedTask.Exception.Message);
            else
            {
                var result = await completedTask;
                respond(result);
            }
        });
        return Task.CompletedTask;
    }

    private async Task CreateAsync(ProtoCreateWorkflowInstanceRequest request)
    {
        if (_workflowInstanceId != null)
        {
            if(_workflowInstanceId == request.WorkflowInstanceId || request.WorkflowInstanceId == null)
                return;
            
            throw new InvalidOperationException($"Attempted to create a new workflow instance with ID {request.WorkflowInstanceId} while an instance with ID {_workflowInstanceId} is already running.");
        }

        _workflowHost = await CreateNewWorkflowHostAsync(request, Context.CancellationToken);
        _workflowInstanceId = _workflowHost.WorkflowState.Id;
    }

    private async Task<ProtoRunWorkflowInstanceResponse> CreateAndRunAsync(ProtoCreateAndRunWorkflowInstanceRequest request)
    {
        var createRequest = new ProtoCreateWorkflowInstanceRequest
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            ParentId = request.ParentId,
            Properties = request.Properties,
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle
        };

        await CreateAsync(createRequest);

        var runWorkflowOptions = new RunWorkflowOptions
        {
            ActivityHandle = _mappers.ActivityHandleMapper.Map(request.ActivityHandle),
            Properties = request.Properties.DeserializeProperties(),
            Input = request.Input.DeserializeInput(),
            CorrelationId = request.CorrelationId,
            TriggerActivityId = request.TriggerActivityId
        };

        var result = await RunAsync(runWorkflowOptions);

        return _mappers.RunWorkflowInstanceResponseMapper.Map(result);
    }

    public override Task Stop()
    {
        // ReSharper disable once MethodHasAsyncOverload
        // Don't use PoisonAsync from within the same actor to avoid deadlocks.
        Context.Poison(Context.Self);
        return Task.CompletedTask;
    }

    public override async Task Cancel()
    {
        var workflowHost = await GetWorkflowHostAsync();
        await workflowHost.CancelWorkflowAsync(Context.CancellationToken);
    }

    public override async Task<ProtoExportWorkflowStateResponse> ExportState()
    {
        var workflowHost = await GetWorkflowHostAsync();
        var workflowState = workflowHost.WorkflowState;
        var json = _mappers.WorkflowStateJsonMapper.Map(workflowState);
        return new ProtoExportWorkflowStateResponse
        {
            SerializedWorkflowState = json
        };
    }

    public override async Task ImportState(ProtoImportWorkflowStateRequest request)
    {
        var workflowState = _mappers.WorkflowStateJsonMapper.Map(request.SerializedWorkflowState);
        var workflowHost = await GetWorkflowHostAsync();
        workflowHost.WorkflowState = workflowState;
        await workflowHost.PersistStateAsync(Context.CancellationToken);
    }

    private async Task<RunWorkflowResult> RunAsync(RunWorkflowOptions runWorkflowOptions)
    {
        var workflowHost = await GetWorkflowHostAsync();
        var result = await workflowHost.RunWorkflowAsync(runWorkflowOptions, Context.CancellationToken);
        return result;
    }

    private async Task SaveSnapshotAsync()
    {
        var workflowState = _workflowHost?.WorkflowState;
        if (workflowState?.Status == WorkflowStatus.Finished)
            await _persistence.DeleteSnapshotsAsync(_persistence.Index);
        else
            await _persistence.PersistSnapshotAsync(GetState());
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        _workflowInstanceId = ((WorkflowGrainSnapshot)snapshot.State).WorkflowInstanceId;
    }

    private object GetState()
    {
        return new WorkflowGrainSnapshot(_workflowInstanceId ?? throw new InvalidOperationException("Workflow instance ID is null."));
    }

    private async Task<IWorkflowHost> GetWorkflowHostAsync()
    {
        if (_workflowHost != null)
            return _workflowHost;

        if (_workflowInstanceId == null)
        {
            // Parse the cluster identity to get the workflow instance ID.
            var clusterIdentity = Context.ClusterIdentity()!.Identity;
            _workflowInstanceId = clusterIdentity.Split('-').Last();
        }

        _workflowHost = await CreateExistingWorkflowHostAsync(_workflowInstanceId, Context.CancellationToken);
        return _workflowHost;
    }

    private async Task<IWorkflowHost> CreateNewWorkflowHostAsync(ProtoCreateWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        var workflowDefinitionHandle = _mappers.WorkflowDefinitionHandleMapper.Map(request.WorkflowDefinitionHandle);
        var workflowInstanceId = request.WorkflowInstanceId;
        var workflowGraph = await FindWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);
        var workflowInstanceOptions = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = workflowInstanceId.NullIfEmpty(),
            CorrelationId = request.CorrelationId.NullIfEmpty(),
            Input = request.Input.DeserializeInput(),
            Properties = request.Properties.DeserializeProperties(),
            ParentWorkflowInstanceId = request.ParentId
        };

        await using var scope = _scopeFactory.CreateAsyncScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var workflowInstance = await workflowInstanceManager.CreateWorkflowInstanceAsync(workflowGraph.Workflow, workflowInstanceOptions, cancellationToken);
        var host = await _workflowHostFactory.CreateAsync(workflowGraph, workflowInstance.WorkflowState, cancellationToken);
        _workflowInstanceId = host.WorkflowState.Id;
        await SaveSnapshotAsync();
        return host;
    }

    private async Task<IWorkflowHost> CreateExistingWorkflowHostAsync(string instanceId, CancellationToken cancellationToken)
    {
        var workflowInstance = await FindWorkflowInstanceAsync(instanceId, cancellationToken);

        if (workflowInstance == null)
            throw new InvalidOperationException($"Workflow instance {instanceId} not found.");

        return await CreateWorkflowHostAsync(workflowInstance, cancellationToken);
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var workflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        var workflow = await FindWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);
        return await _workflowHostFactory.CreateAsync(workflow, workflowInstance.WorkflowState, cancellationToken);
    }

    private async Task<WorkflowInstance?> FindWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        return await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);
    }

    private async Task<WorkflowGraph> FindWorkflowGraphAsync(WorkflowDefinitionHandle workflowDefinitionHandle, CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        if (workflow == null)
            throw new InvalidOperationException($"Workflow {workflowDefinitionHandle} not found.");

        return workflow;
    }
}