using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using WorkflowDefinitionHandle = Elsa.Workflows.Models.WorkflowDefinitionHandle;

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
    private CancellationTokenSource _linkedTokenSource = null!;
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
        _linkedTokenSource = new();
        _linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(Context.CancellationToken, _linkedTokenSource.Token).Token;
        return Task.CompletedTask;
    }

    public override Task OnStopped()
    {
        _linkedTokenSource.Dispose();
        return Task.CompletedTask;
    }

    public override Task<CreateWorkflowInstanceResponse> Create(CreateWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task Create(CreateWorkflowInstanceRequest request, Action<CreateWorkflowInstanceResponse> respond, Action<string> onError)
    {
        Context.ReenterAfter(CreateAsync(request), result =>
        {
            if (result.IsFaulted)
                onError(result.Exception.Message);
            else
                respond(new());
        });

        return Task.CompletedTask;
    }

    public override Task<RunWorkflowInstanceResponse> Run(RunWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task Run(RunWorkflowInstanceRequest request, Action<RunWorkflowInstanceResponse> respond, Action<string> onError)
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

    public override Task<RunWorkflowInstanceResponse> CreateAndRun(CreateAndRunWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task CreateAndRun(CreateAndRunWorkflowInstanceRequest request, Action<RunWorkflowInstanceResponse> respond, Action<string> onError)
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

    private async Task CreateAsync(CreateWorkflowInstanceRequest request)
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

    private async Task<RunWorkflowInstanceResponse> CreateAndRunAsync(CreateAndRunWorkflowInstanceRequest request)
    {
        var createRequest = new CreateWorkflowInstanceRequest
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
            CorrelationId = request.CorrelationId.NullIfEmpty(),
            TriggerActivityId = request.TriggerActivityId.NullIfEmpty()
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
        WorkflowState = await workflowCanceler.CancelWorkflowAsync(WorkflowGraph, WorkflowState, Context.CancellationToken);
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, Context.CancellationToken);
    }

    public override async Task<ExportWorkflowStateResponse> ExportState()
    {
        await EnsureStateAsync();
        var json = mappers.WorkflowStateJsonMapper.Map(WorkflowState);
        return new()
        {
            SerializedWorkflowState = json
        };
    }

    public override async Task ImportState(ImportWorkflowStateRequest request)
    {
        var workflowState = mappers.WorkflowStateJsonMapper.Map(request.SerializedWorkflowState);
        await EnsureStateAsync();
        WorkflowState = workflowState;
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, Context.CancellationToken);
    }

    public override Task<InstanceExistsResponse> InstanceExists()
    {
        var exists = _workflowInstanceId != null;
        return Task.FromResult(new InstanceExistsResponse
        {
            Exists = exists
        });
    }

    private async Task<RunWorkflowResult> RunAsync(RunWorkflowOptions runWorkflowOptions)
    {
        if (_isRunning)
        {
            _queuedRunWorkflowOptions.Enqueue(runWorkflowOptions);
            return new(null!, null!, null!, null, Journal.Empty);
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
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, Context.CancellationToken);

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

    private async Task CreateNewWorkflowInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken)
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
            ParentWorkflowInstanceId = request.ParentId.NullIfEmpty()
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var workflowInstance = await workflowInstanceManager.CreateAndCommitWorkflowInstanceAsync(workflowGraph.Workflow, workflowInstanceOptions, cancellationToken);
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