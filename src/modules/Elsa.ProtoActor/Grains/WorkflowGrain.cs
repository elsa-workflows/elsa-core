using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.ProtoActor.Snapshots;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
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
    private string _workflowDefinitionVersionId = default!;
    private string _workflowInstanceId = default!;
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

    public override Task<ProtoExecuteWorkflowResponse> ExecuteAndWait(ProtoExecuteWorkflowRequest request) => throw new NotImplementedException();

    public override Task ExecuteAndWait(ProtoExecuteWorkflowRequest request, Action<ProtoExecuteWorkflowResponse> respond, Action<string> onError)
    {
        var task = ExecuteAndWaitAsync(request);
        Context.ReenterAfter(task, async executeTask =>
        {
            var result = await executeTask;
            respond(result);
        });
        return Task.CompletedTask;
    }

    public override Task ExecuteAndForget(ProtoExecuteWorkflowRequest request) => throw new NotImplementedException();

    public override Task ExecuteAndForget(ProtoExecuteWorkflowRequest request, Action respond, Action<string> onError)
    {
        var task = ExecuteAndWaitAsync(request);
        Context.ReenterAfter(task, async executeTask =>
        {
            await executeTask;
            respond();
        });

        return Task.CompletedTask;
    }

    public override Task Stop()
    {
        // Stop after all current messages have been processed.
        // Calling StopAsync seems to cause a deadlock or some other issue where the call never returns. See also: https://github.com/asynkron/protoactor-dotnet/issues/492
        Context.Stop(Context.Self);
        return Task.CompletedTask;
    }

    public override async Task Cancel()
    {
        var workflowHost = GetWorkflowHost();
        await workflowHost.CancelWorkflowAsync(Context.CancellationToken);
    }

    public override async Task<ProtoExportWorkflowStateResponse> ExportState()
    {
        var workflowHost = GetWorkflowHost();
        var workflowState = workflowHost.WorkflowState;
        var json = await _mappers.WorkflowStateJsonMapper.MapAsync(workflowState, Context.CancellationToken);
        return new ProtoExportWorkflowStateResponse
        {
            SerializedWorkflowState = json
        };
    }

    public override async Task ImportState(ProtoImportWorkflowStateRequest request)
    {
        var workflowState = await _mappers.WorkflowStateJsonMapper.MapAsync(request.SerializedWorkflowState, Context.CancellationToken);
        var workflowHost = GetWorkflowHost();
        workflowHost.WorkflowState = workflowState;
        await workflowHost.PersistStateAsync(Context.CancellationToken);
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        (_workflowDefinitionVersionId, _workflowInstanceId) = (WorkflowGrainSnapshot)snapshot.State;
    }

    private async Task SaveSnapshotAsync()
    {
        var workflowState = _workflowHost?.WorkflowState;
        if (workflowState?.Status == WorkflowStatus.Finished)
            await _persistence.DeleteSnapshotsAsync(_persistence.Index);
        else
            await _persistence.PersistSnapshotAsync(GetState());
    }

    private object GetState() => new WorkflowGrainSnapshot(_workflowDefinitionVersionId, _workflowInstanceId);

    private async Task<IWorkflowHost> GetOrCreateWorkflowHostAsync(ProtoExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (_workflowHost == null)
        {
            var workflowHost = await CreateWorkflowHostAsync(request, cancellationToken);
            _workflowHost = workflowHost;
            _workflowDefinitionVersionId = workflowHost.Workflow.Identity.Id;
            _workflowInstanceId = workflowHost.WorkflowState.Id;
        }

        return _workflowHost;
    }

    private IWorkflowHost GetWorkflowHost()
    {
        return _workflowHost ?? throw new InvalidOperationException("Workflow host not initialized.");
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(ProtoExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (request.IsNewInstance)
            return await CreateWorkflowHostAsync(request.WorkflowDefinitionVersionId, request.WorkflowInstanceId.NullIfEmpty(), cancellationToken);

        var workflowInstance = await FindWorkflowInstanceAsync(request.WorkflowInstanceId, cancellationToken);

        if (workflowInstance == null)
            throw new InvalidOperationException($"Workflow instance {request.WorkflowInstanceId} not found.");

        return await CreateWorkflowHostAsync(workflowInstance, cancellationToken);
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var workflowDefinitionVersionId = workflowInstance.DefinitionVersionId;
        var workflow = await FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);

        if (workflow == null)
            throw new InvalidOperationException($"Workflow {workflowDefinitionVersionId} not found.");

        return await _workflowHostFactory.CreateAsync(workflow, workflowInstance.WorkflowState, cancellationToken);
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(string workflowDefinitionVersionId, string? workflowInstanceId, CancellationToken cancellationToken)
    {
        var workflow = await FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);

        if (workflow == null)
            throw new InvalidOperationException($"Workflow {workflowDefinitionVersionId} not found.");

        var host = await _workflowHostFactory.CreateAsync(workflow, workflowInstanceId, cancellationToken);

        // Save a snapshot of the workflow host. This ensures that the workflow host can be restored in case of a crash.
        _workflowDefinitionVersionId = host.Workflow.Identity.Id;
        _workflowInstanceId = host.WorkflowState.Id;
        await SaveSnapshotAsync();

        return host;
    }

    private async Task<WorkflowInstance?> FindWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        return await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);
    }

    private async Task<Workflow?> FindWorkflowAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        return await workflowDefinitionService.FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);
    }
    
    private async Task<ProtoExecuteWorkflowResponse> ExecuteAndWaitAsync(ProtoExecuteWorkflowRequest request)
    {
        var workflowHost = await GetOrCreateWorkflowHostAsync(request, Context.CancellationToken);
        var mappedRequest = _mappers.ExecuteWorkflowRequestMapper.Map(request);
        var result = await workflowHost.ExecuteWorkflowAsync(mappedRequest, Context.CancellationToken);
        return _mappers.ExecuteWorkflowResponseMapper.Map(result);
    }
}