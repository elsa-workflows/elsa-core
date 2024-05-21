using System.Diagnostics;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.ProtoActor.Snapshots;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Persistence;

namespace Elsa.ProtoActor.Actors;

internal class WorkflowInstanceImpl : WorkflowInstanceBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Mappers.Mappers _mappers;
    private readonly Persistence _persistence;
    private string? _workflowInstanceId;
    private WorkflowGraph? _workflowGraph;
    private WorkflowState? _workflowState;
    private CancellationTokenSource _linkedTokenSource = default!;
    private CancellationToken _linkedCancellationToken;
    private readonly Queue<RunWorkflowOptions> _queuedRunWorkflowOptions = new();
    private bool _isRunning;

    /// <inheritdoc />
    public WorkflowInstanceImpl(
        IContext context,
        IServiceScopeFactory scopeFactory,
        IProvider provider,
        Mappers.Mappers mappers) : base(context)
    {
        _scopeFactory = scopeFactory;
        _mappers = mappers;
        _persistence = Persistence.WithSnapshotting(provider, context.ClusterIdentity()!.Identity, ApplySnapshot);
    }

    private WorkflowGraph WorkflowGraph
    {
        get => _workflowGraph!;
        set => _workflowGraph = value;
    }

    private WorkflowState WorkflowState
    {
        get => _workflowState!;
        set => _workflowState = value;
    }

    public override async Task OnStarted()
    {
        _linkedTokenSource = new CancellationTokenSource();
        _linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(Context.CancellationToken, _linkedTokenSource.Token).Token;
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
            // If a workflow instance ID is already set, this is the cluster trying to repeat this call because it was too slow.
            // This happens when debugging and the debugger is paused for a long time, for example.
            if (_workflowInstanceId == request.WorkflowInstanceId || request.WorkflowInstanceId == null)
                return;

            throw new InvalidOperationException($"Attempted to create a new workflow instance with ID {request.WorkflowInstanceId} while an instance with ID {_workflowInstanceId} is already running.");
        }

        await CreateNewWorkflowInstanceAsync(request, Context.CancellationToken);
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
        await EnsureStateAsync();
        _linkedTokenSource.Cancel();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var workflowCanceler = serviceProvider.GetRequiredService<IWorkflowCanceler>();
        _workflowState = await workflowCanceler.CancelWorkflowAsync(WorkflowGraph, WorkflowState, Context.CancellationToken);
        await PersistStateAsync(scope, Context.CancellationToken);
    }

    public override async Task<ProtoExportWorkflowStateResponse> ExportState()
    {
        await EnsureStateAsync();
        var json = _mappers.WorkflowStateJsonMapper.Map(WorkflowState);
        return new ProtoExportWorkflowStateResponse
        {
            SerializedWorkflowState = json
        };
    }

    public override async Task ImportState(ProtoImportWorkflowStateRequest request)
    {
        var workflowState = _mappers.WorkflowStateJsonMapper.Map(request.SerializedWorkflowState);
        await EnsureStateAsync();
        WorkflowState = workflowState;
        await PersistStateAsync(Context.CancellationToken);
    }

    private async Task<RunWorkflowResult> RunAsync(RunWorkflowOptions runWorkflowOptions)
    {
        if(_isRunning)
        {
            _queuedRunWorkflowOptions.Enqueue(runWorkflowOptions);
            return new RunWorkflowResult(default, default, null);
        }
        
        _isRunning = true;
        var workflowResult = await RunInternalAsync(runWorkflowOptions);
        _isRunning = false;

         if (_queuedRunWorkflowOptions.Count > 0)
         {
             var nextRunOptions = _queuedRunWorkflowOptions.Dequeue();
             return await RunAsync(nextRunOptions);
         }

        return workflowResult;
    }

    private async Task<RunWorkflowResult> RunInternalAsync(RunWorkflowOptions runWorkflowOptions)
    {
        await EnsureStateAsync();
        runWorkflowOptions.WorkflowInstanceId = _workflowInstanceId;
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = await workflowRunner.RunAsync(WorkflowGraph, WorkflowState, runWorkflowOptions, _linkedCancellationToken);
        
        if(!string.IsNullOrEmpty(runWorkflowOptions.BookmarkId))
        {
            if (workflowResult.WorkflowState.Bookmarks.Any())
            {
                Debugger.Break();
            }
        }
        
        WorkflowState = workflowResult.WorkflowState;
        
        await PersistStateAsync(scope, Context.CancellationToken);
        return workflowResult;
    }

    private async Task SaveSnapshotAsync()
    {
        // var workflowState = _workflowState;
        // if (workflowState?.Status == WorkflowStatus.Finished)
        //     await _persistence.DeleteSnapshotsAsync(_persistence.Index);
        // else
        //     await _persistence.PersistSnapshotAsync(GetState());
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        _workflowInstanceId = ((WorkflowGrainSnapshot)snapshot.State).WorkflowInstanceId;
    }

    private object GetState()
    {
        return new WorkflowGrainSnapshot(_workflowInstanceId ?? throw new InvalidOperationException("Workflow instance ID is null."));
    }

    private async Task EnsureStateAsync()
    {
        if (_workflowState != null)
            return;

        if (_workflowInstanceId == null)
        {
            // Parse the cluster identity to get the workflow instance ID.
            var clusterIdentity = Context.ClusterIdentity()!.Identity;
            _workflowInstanceId = clusterIdentity.Split('-').Last();
        }

        var workflowInstance = await FindWorkflowInstanceAsync(_workflowInstanceId, Context.CancellationToken);

        if (workflowInstance == null)
            throw new InvalidOperationException($"Workflow instance {_workflowInstanceId} not found.");

        var workflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        var workflowGraph = await FindWorkflowGraphAsync(workflowDefinitionHandle, Context.CancellationToken);

        WorkflowGraph = workflowGraph;
        WorkflowState = workflowInstance.WorkflowState;
    }

    private async Task CreateNewWorkflowInstanceAsync(ProtoCreateWorkflowInstanceRequest request, CancellationToken cancellationToken)
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
        var workflowState = workflowInstance.WorkflowState;
        _workflowInstanceId = workflowState.Id;
        WorkflowGraph = workflowGraph;
        WorkflowState = workflowInstance.WorkflowState;
        await SaveSnapshotAsync();
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

    private async Task PersistStateAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        await PersistStateAsync(scope, cancellationToken);
    }

    private async Task PersistStateAsync(IServiceScope scope, CancellationToken cancellationToken = default)
    {
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.UpdateAsync(WorkflowState, cancellationToken);
    }
}