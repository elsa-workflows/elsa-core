using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;

namespace Elsa.Workflows.Runtime.ProtoActor.Actors;

internal class WorkflowInstance(
    IContext context,
    IServiceScopeFactory scopeFactory,
    Mappers.Mappers mappers)
    : WorkflowInstanceBase(context)
{
    private string? _workflowInstanceId;
    private WorkflowGraph? _workflowGraph;
    private WorkflowState? _workflowState;
    private CancellationTokenSource _linkedTokenSource = default!;
    private CancellationToken _linkedCancellationToken;
    private readonly Queue<RunWorkflowOptions> _queuedRunWorkflowOptions = new();
    private bool _isRunning;


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

    public override Task OnStarted()
    {
        _linkedTokenSource = new CancellationTokenSource();
        _linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(Context.CancellationToken, _linkedTokenSource.Token).Token;
        return Task.CompletedTask;
    }

    public override Task OnStopped()
    {
        _linkedTokenSource.Dispose();
        return Task.CompletedTask;
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
        var mappedRequest = mappers.RunWorkflowParamsMapper.Map(request);
        Context.ReenterAfter(RunAsync(mappedRequest), async completedTask =>
        {
            if (completedTask.IsFaulted)
                onError(completedTask.Exception.Message);
            else
            {
                var result = await completedTask;
                respond(mappers.RunWorkflowInstanceResponseMapper.Map(result));
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
            ActivityHandle = mappers.ActivityHandleMapper.Map(request.ActivityHandle),
            Properties = request.Properties.DeserializeProperties(),
            Input = request.Input.DeserializeInput(),
            CorrelationId = request.CorrelationId,
            TriggerActivityId = request.TriggerActivityId
        };

        var result = await RunAsync(runWorkflowOptions);

        return mappers.RunWorkflowInstanceResponseMapper.Map(result);
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
        await using var scope = scopeFactory.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var workflowCanceler = serviceProvider.GetRequiredService<IWorkflowCanceler>();
        _workflowState = await workflowCanceler.CancelWorkflowAsync(WorkflowGraph, WorkflowState, Context.CancellationToken);
    }

    public override async Task<ProtoExportWorkflowStateResponse> ExportState()
    {
        await EnsureStateAsync();
        var json = mappers.WorkflowStateJsonMapper.Map(WorkflowState);
        return new ProtoExportWorkflowStateResponse
        {
            SerializedWorkflowState = json
        };
    }

    public override async Task ImportState(ProtoImportWorkflowStateRequest request)
    {
        var workflowState = mappers.WorkflowStateJsonMapper.Map(request.SerializedWorkflowState);
        await EnsureStateAsync();
        WorkflowState = workflowState;
    }

    private async Task<RunWorkflowResult> RunAsync(RunWorkflowOptions runWorkflowOptions)
    {
        if (_isRunning)
        {
            _queuedRunWorkflowOptions.Enqueue(runWorkflowOptions);
            return new RunWorkflowResult(null!, null!, null);
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

        await using var scope = scopeFactory.CreateAsyncScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = await workflowRunner.RunAsync(WorkflowGraph, WorkflowState, runWorkflowOptions, _linkedCancellationToken);
        WorkflowState = workflowResult.WorkflowState;

        return workflowResult;
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
        var workflowDefinitionHandle = mappers.WorkflowDefinitionHandleMapper.Map(request.WorkflowDefinitionHandle);
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

        await using var scope = scopeFactory.CreateAsyncScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var workflowInstance = await workflowInstanceManager.CreateWorkflowInstanceAsync(workflowGraph.Workflow, workflowInstanceOptions, cancellationToken);
        var workflowState = workflowInstance.WorkflowState;
        _workflowInstanceId = workflowState.Id;
        WorkflowGraph = workflowGraph;
        WorkflowState = workflowInstance.WorkflowState;
    }

    private async Task<Management.Entities.WorkflowInstance?> FindWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        return await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);
    }

    private async Task<WorkflowGraph> FindWorkflowGraphAsync(WorkflowDefinitionHandle workflowDefinitionHandle, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        if (workflow == null)
            throw new InvalidOperationException($"Workflow {workflowDefinitionHandle} not found.");

        return workflow;
    }
}