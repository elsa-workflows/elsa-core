using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
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
            if (!result.IsCompletedSuccessfully)
                onError(WorkflowInstanceRpcErrors.FromCreateFailure(result));
            else
                respond(new());
        });

        return Task.CompletedTask;
    }

    public override Task<RunWorkflowInstanceResponse> Run(RunWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task Run(RunWorkflowInstanceRequest request, Action<RunWorkflowInstanceResponse> respond, Action<string> onError)
    {
        var mappedRequest = mappers.RunWorkflowParamsMapper.Map(request);
        var includeOutput = request.IncludeWorkflowOutput;
        Context.ReenterAfter(RunAsync(mappedRequest), async completedTask =>
        {
            if (completedTask.IsFaulted)
                onError(completedTask.Exception.Message);
            else
            {
                var result = await completedTask;
                respond(mappers.RunWorkflowInstanceResponseMapper.Map(result, includeOutput));
            }
        });

        return Task.CompletedTask;
    }

    public override Task<RunWorkflowInstanceResponse> CreateAndRun(CreateAndRunWorkflowInstanceRequest request) => throw new NotImplementedException();

    public override Task CreateAndRun(CreateAndRunWorkflowInstanceRequest request, Action<RunWorkflowInstanceResponse> respond, Action<string> onError)
    {
        Context.ReenterAfter(CreateAndRunAsync(request), async completedTask =>
        {
            if (!completedTask.IsCompletedSuccessfully)
                onError(WorkflowInstanceRpcErrors.FromCreateFailure(completedTask));
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
        var workflowInstanceId = GetWorkflowInstanceId();
        if (!string.IsNullOrEmpty(request.WorkflowInstanceId) && request.WorkflowInstanceId != workflowInstanceId)
            throw new InvalidOperationException("The workflow instance identifier must match the actor identity.");

        if (HasState())
            throw new WorkflowInstanceAlreadyExistsException(workflowInstanceId);

        await CreateNewWorkflowInstanceAsync(request, workflowInstanceId, Context.CancellationToken);
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

        return mappers.RunWorkflowInstanceResponseMapper.Map(result, request.IncludeWorkflowOutput);
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
        using var executionScope = WorkflowExecutionScope.Begin(WorkflowState.Id);
        _linkedTokenSource.Cancel();
        await using var scope = scopeFactory.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var workflowCanceler = serviceProvider.GetRequiredService<IWorkflowCanceler>();
        WorkflowState = await workflowCanceler.CancelWorkflowAsync(WorkflowGraph, WorkflowState, Context.CancellationToken);
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, Context.CancellationToken);
    }

    public override async Task Delete()
    {
        if (HasState()) 
            _linkedTokenSource.Cancel();
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var workflowInstanceManager = serviceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var workflowInstanceId = GetWorkflowInstanceId();
        var filter = new WorkflowInstanceFilter { Id = workflowInstanceId };
        await workflowInstanceManager.DeleteAsync(filter, Context.CancellationToken);
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
        var workflowGraph = WorkflowGraph;

        if (workflowGraph.Workflow.Identity.Id != workflowState.DefinitionVersionId)
        {
            var workflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowState.DefinitionVersionId);
            workflowGraph = await FindWorkflowGraphAsync(workflowDefinitionHandle, Context.CancellationToken);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(workflowState, Context.CancellationToken);
        WorkflowGraph = workflowGraph;
        WorkflowState = workflowState;
    }

    public override async Task<InstanceExistsResponse> InstanceExists()
    {
        var workflowInstanceId = GetWorkflowInstanceId();
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        return new()
        {
            Exists = await workflowInstanceManager.ExistsAsync(workflowInstanceId, Context.CancellationToken)
        };
    }

    public override async Task<RunIfPendingWorkflowInstanceResponse> RunIfPending(RunWorkflowInstanceRequest request)
    {
        if (_isRunning)
            return new();

        var result = await RunExclusiveAsync(mappers.RunWorkflowParamsMapper.Map(request), onlyPending: true,
            request.HasStartDeliveryId ? request.StartDeliveryId.NullIfEmpty() : null);
        return new()
        {
            Execution = result != null ? mappers.RunWorkflowInstanceResponseMapper.Map(result, request.IncludeWorkflowOutput) : null
        };
    }

    private async Task<RunWorkflowResult> RunAsync(RunWorkflowOptions runWorkflowOptions)
    {
        if (_isRunning)
        {
            _queuedRunWorkflowOptions.Enqueue(runWorkflowOptions);
            return new(null!, null!, null!, null, Journal.Empty);
        }

        return (await RunExclusiveAsync(runWorkflowOptions, onlyPending: false, deliveryId: null))!;
    }

    private async Task<RunWorkflowResult?> RunExclusiveAsync(RunWorkflowOptions options, bool onlyPending, string? deliveryId)
    {
        RunWorkflowResult? result = null;
        _isRunning = true;

        try
        {
            await EnsureStateAsync();
            if (!onlyPending || (WorkflowState.SubStatus == WorkflowSubStatus.Pending && WorkflowStartDelivery.Matches(WorkflowState, deliveryId)))
                result = await RunInternalAsync(options);
        }
        finally
        {
            _isRunning = false;
        }

        if (_queuedRunWorkflowOptions.TryDequeue(out var next))
        {
            var nextResult = await RunAsync(next);
            return onlyPending ? result : nextResult;
        }

        return result;
    }

    private async Task<RunWorkflowResult> RunInternalAsync(RunWorkflowOptions runWorkflowOptions)
    {
        using var executionScope = WorkflowExecutionScope.Begin(WorkflowState.Id);
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
        if (HasState())
            return;

        var workflowInstanceId = GetWorkflowInstanceId();
        var workflowInstance = await FindWorkflowInstanceAsync(workflowInstanceId, Context.CancellationToken);

        if (workflowInstance == null)
            throw new InvalidOperationException($"Workflow instance {workflowInstanceId} not found.");

        var workflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        var workflowGraph = await FindWorkflowGraphAsync(workflowDefinitionHandle, Context.CancellationToken);

        WorkflowGraph = workflowGraph;
        WorkflowState = workflowInstance.WorkflowState;
    }

    private bool HasState()
    {
        return _workflowState != null;
    }

    private string GetWorkflowInstanceId()
    {
        if (_workflowInstanceId != null)
            return _workflowInstanceId;

        var clusterIdentity = Context.ClusterIdentity()!.Identity;
        _workflowInstanceId = Elsa.Extensions.ClusterExtensions.GetWorkflowInstanceId(clusterIdentity);

        return _workflowInstanceId;
    }
    

    private async Task CreateNewWorkflowInstanceAsync(CreateWorkflowInstanceRequest request, string workflowInstanceId, CancellationToken cancellationToken)
    {
        var workflowDefinitionHandle = mappers.WorkflowDefinitionHandleMapper.Map(request.WorkflowDefinitionHandle);
        var workflowGraph = await FindWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);
        var workflowInstanceOptions = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = workflowInstanceId,
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
