using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Mappers;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.ProtoActor.Snapshots;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using CancellationTokens = Elsa.Workflows.Models.CancellationTokens;
using WorkflowStatus = Elsa.Workflows.WorkflowStatus;
using WorkflowSubStatus = Elsa.Workflows.WorkflowSubStatus;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Executes a workflow.
/// </summary>
internal class WorkflowInstance : WorkflowInstanceBase
{
    private const int MaxSnapshotsToKeep = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkflowStateMapper _workflowStateMapper;
    private readonly WorkflowStatusMapper _workflowStatusMapper;
    private readonly WorkflowSubStatusMapper _workflowSubStatusMapper;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly Persistence _persistence;

    private string _definitionId = default!;
    private string _instanceId = default!;
    private int _version;
    private IDictionary<string, object>? _input;
    private IDictionary<string, object>? _properties;
    private IWorkflowHost _workflowHost = default!;
    private WorkflowState _workflowState = default!;

    private readonly ICollection<CancellationTokenSource> _cancellationTokenSources = new List<CancellationTokenSource>();

    /// <inheritdoc />
    public WorkflowInstance(
        IServiceScopeFactory scopeFactory,
        IProvider provider,
        IContext context,
        WorkflowStateMapper workflowStateMapper,
        WorkflowStatusMapper workflowStatusMapper,
        WorkflowSubStatusMapper workflowSubStatusMapper,
        IWorkflowInstanceStore workflowInstanceStore
    ) : base(context)
    {
        _scopeFactory = scopeFactory;
        _workflowStateMapper = workflowStateMapper;
        _workflowStatusMapper = workflowStatusMapper;
        _workflowSubStatusMapper = workflowSubStatusMapper;
        _workflowInstanceStore = workflowInstanceStore;
        _persistence = Persistence.WithSnapshotting(provider, context.ClusterIdentity()!.Identity, ApplySnapshot);
    }

    /// <inheritdoc />
    public override async Task OnStarted()
    {
        await _persistence.RecoverStateAsync();

        if (string.IsNullOrWhiteSpace(_definitionId))
            return; // No state yet to recover from.

        var cancellationToken = Context.CancellationToken;
        using var scope = _scopeFactory.CreateScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();

        // Get the workflow.
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(_definitionId, VersionOptions.SpecificVersion(_version), cancellationToken);

        if (workflowGraph == null)
            throw new Exception("Workflow definition is no longer available");

        var workflow = workflowGraph.Workflow;
        
        // Create an initial workflow state.
        if (_workflowState == null!)
        {
            _workflowState = new WorkflowState
            {
                DefinitionId = workflow.Identity.DefinitionId,
                DefinitionVersion = workflow.Identity.Version,
            };
        }

        // Create a workflow host.
        var workflowHostFactory = scope.ServiceProvider.GetRequiredService<IWorkflowHostFactory>();
        _workflowHost = await workflowHostFactory.CreateAsync(workflowGraph, _workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<CanStartWorkflowResponse> CanStart(StartWorkflowRequest request) => Task.FromResult(new CanStartWorkflowResponse());

    /// <inheritdoc />
    public override async Task CanStart(StartWorkflowRequest request, Action<CanStartWorkflowResponse> respond, Action<string> onError)
    {
        var definitionId = request.DefinitionId;
        var instanceId = request.InstanceId;
        var isExistingInstance = request.IsExistingInstance;
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var correlationId = request.CorrelationId.NullIfEmpty();
        var input = request.Input?.Deserialize();
        var properties = request.Properties?.Deserialize();
        var cancellationToken = Context.CancellationToken;
        var startWorkflowOptions = new StartWorkflowHostParams
        {
            InstanceId = instanceId,
            IsExistingInstance = request.IsExistingInstance,
            CorrelationId = correlationId,
            Input = input,
            Properties = properties,
            TriggerActivityId = request.TriggerActivityId
        };

        _workflowHost = await CreateWorkflowHostAsync(
            definitionId,
            versionOptions,
            request.InstanceId,
            isExistingInstance,
            cancellationToken);

        _version = _workflowHost.Workflow.Identity.Version;
        _definitionId = definitionId;
        _instanceId = instanceId;
        _input = input;

        var task = _workflowHost.CanStartWorkflowAsync(startWorkflowOptions, cancellationToken);

        Context.ReenterAfter(task, async canStart =>
        {
            respond(new CanStartWorkflowResponse
            {
                CanStart = await canStart
            });
        });
    }

    /// <inheritdoc />
    public override Task<WorkflowExecutionResponse> Start(StartWorkflowRequest request) => Task.FromResult(new WorkflowExecutionResponse());

    /// <inheritdoc />
    public override async Task Start(StartWorkflowRequest request, Action<WorkflowExecutionResponse> respond, Action<string> onError)
    {
        var definitionId = request.DefinitionId;
        var instanceId = request.InstanceId;
        var isExistingInstance = request.IsExistingInstance;
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var correlationId = request.CorrelationId.NullIfEmpty();
        var input = request.Input?.Deserialize();
        var properties = request.Properties?.Deserialize();
        var cancellationToken = Context.CancellationToken;

        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokenSources.Add(cancellationTokenSource);
        cancellationToken = cancellationTokenSource.Token;

        // Only need to reconstruct a workflow host if not already done so during CanStart.
        if (_workflowHost == null!)
        {
            _workflowHost = await CreateWorkflowHostAsync(definitionId, versionOptions, instanceId, isExistingInstance, cancellationToken);
            _version = _workflowHost.Workflow.Identity.Version;
            _definitionId = definitionId;
            _instanceId = instanceId;
            _input = input;
        }

        var startWorkflowOptions = new StartWorkflowHostParams
        {
            InstanceId = instanceId,
            IsExistingInstance = request.IsExistingInstance,
            CorrelationId = correlationId,
            Input = input,
            Properties = properties,
            TriggerActivityId = request.TriggerActivityId,
            StatusUpdatedCallback = StatusUpdated,
            CancellationTokens = new CancellationTokens(cancellationToken)
        };

        var task = _workflowHost.StartWorkflowAsync(startWorkflowOptions, cancellationToken);

        Context.ReenterAfter(task, async startWorkflowResultTask =>
        {
            var startWorkflowResult = await startWorkflowResultTask;
            var workflowState = _workflowHost.WorkflowState;
            var result = workflowState.Status == WorkflowStatus.Finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended;

            _workflowState = workflowState;

            await SaveSnapshotAsync();
            SaveWorkflowInstance(workflowState);

            using var scope = _scopeFactory.CreateScope();
            var bookmarkMapper = scope.ServiceProvider.GetRequiredService<BookmarkMapper>();
            var mappedBookmarks = bookmarkMapper.Map(workflowState.Bookmarks).ToList();

            respond(new WorkflowExecutionResponse
            {
                Result = result,
                Bookmarks =
                {
                    mappedBookmarks
                },
                Status = _workflowStatusMapper.Map(workflowState.Status),
                SubStatus = _workflowSubStatusMapper.Map(workflowState.SubStatus),
                TriggeredActivityId = string.Empty,
                WorkflowInstanceId = instanceId
            });
        });
    }

    private void StatusUpdated(WorkflowExecutionContext context)
    {
        if (context.Status == WorkflowStatus.Finished)
        {
            _cancellationTokenSources.Clear();
            return;
        }

        if (context.SubStatus == WorkflowSubStatus.Cancelled)
            _ = Task.Run(async () => await Update(context));
    }

    private async Task Update(WorkflowExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var extractor = scope.ServiceProvider.GetRequiredService<IWorkflowStateExtractor>();
        var bookmarksPersister = scope.ServiceProvider.GetRequiredService<IBookmarksPersister>();
        var workflowState = extractor.Extract(context);
        var originalBookmarks = _workflowHost.WorkflowState.Bookmarks;

        _workflowState = workflowState;

        await SaveSnapshotAsync();
        SaveWorkflowInstance(workflowState);
        var newBookmarks = workflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, newBookmarks);
        var bookmarkRequest = new UpdateBookmarksRequest(workflowState.DefinitionId, diff, workflowState.CorrelationId);
        await bookmarksPersister.PersistBookmarksAsync(bookmarkRequest);
    }

    /// <inheritdoc />
    public override Task Stop()
    {
        // Stop after all current messages have been processed.
        // ReSharper disable once MethodHasAsyncOverload
        // Calling StopAsync seems to cause a deadlock or some other issue where the call never returns. See also: https://github.com/asynkron/protoactor-dotnet/issues/492
        Context.Stop(Context.Self);
        return Task.CompletedTask;
    }

    public override async Task Resume(ResumeWorkflowRequest request, Action<WorkflowExecutionResponse> respond, Action<string> onError)
    {
        _input = request.Input?.Deserialize();
        _properties = request.Properties?.Deserialize();
        var correlationId = request.CorrelationId;
        var bookmarkId = request.BookmarkId.NullIfEmpty();
        var activityId = request.ActivityId.NullIfEmpty();
        var activityNodeId = request.ActivityNodeId.NullIfEmpty();
        var activityInstanceId = request.ActivityInstanceId.NullIfEmpty();
        var activityHash = request.ActivityHash.NullIfEmpty();
        var cancellationToken = Context.CancellationToken;

        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokenSources.Add(cancellationTokenSource);
        cancellationToken = cancellationTokenSource.Token;

        var resumeWorkflowHostOptions = new ResumeWorkflowHostParams
        {
            CorrelationId = correlationId,
            BookmarkId = bookmarkId,
            ActivityId = activityId,
            ActivityNodeId = activityNodeId,
            ActivityInstanceId = activityInstanceId,
            ActivityHash = activityHash,
            Input = _input,
            Properties = _properties,
            CancellationTokens = cancellationToken
        };

        var definitionId = _definitionId;
        var versionOptions = VersionOptions.SpecificVersion(_version);

        // Only need to reconstruct a workflow host if not already done so during CanStart.
        if (_workflowHost == null!)
        {
            _workflowHost = await CreateWorkflowHostAsync(definitionId, versionOptions, cancellationToken: cancellationToken);
            _version = _workflowHost.Workflow.Identity.Version;
        }

        var task = _workflowHost.ResumeWorkflowAsync(resumeWorkflowHostOptions, cancellationToken);

        async void Action()
        {
            var finished = _workflowHost.WorkflowState.Status == WorkflowStatus.Finished;

            _workflowState = _workflowHost.WorkflowState;

            using var scope = _scopeFactory.CreateScope();
            await SaveWorkflowInstanceCoreAsync(scope.ServiceProvider, _workflowState);
            var bookmarkMapper = scope.ServiceProvider.GetRequiredService<BookmarkMapper>();

            var response = new WorkflowExecutionResponse
            {
                Result = finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended,
                Bookmarks =
                {
                    bookmarkMapper.Map(_workflowHost.WorkflowState.Bookmarks).ToList()
                },
                TriggeredActivityId = string.Empty,
                WorkflowInstanceId = _workflowState.Id,
                Status = _workflowStatusMapper.Map(_workflowState.Status),
                SubStatus = _workflowSubStatusMapper.Map(_workflowState.SubStatus)
            };

            respond(response);
        }

        Context.ReenterAfter(task, Action);
    }

    /// <inheritdoc />
    public override Task<WorkflowExecutionResponse> Resume(ResumeWorkflowRequest request) => Task.FromResult(new WorkflowExecutionResponse());

    public override Task<WorkflowInstanceCancellationResponse> Cancel()
    {
        if (_workflowState.Status == WorkflowStatus.Finished)
            return Task.FromResult(new WorkflowInstanceCancellationResponse
            {
                Result = false
            });

        _workflowState.SubStatus = WorkflowSubStatus.Cancelled;
        _workflowState.Status = WorkflowStatus.Finished;

        foreach (var source in _cancellationTokenSources)
            source.Cancel();

        return Task.FromResult(new WorkflowInstanceCancellationResponse
        {
            Result = true
        });
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.SerializeAsync(WorkflowState, CancellationToken)")]
    public override async Task<ExportWorkflowStateResponse> ExportState(ExportWorkflowStateRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var workflowStateSerializer = scope.ServiceProvider.GetRequiredService<IWorkflowStateSerializer>();
        var json = await workflowStateSerializer.SerializeAsync(_workflowHost.WorkflowState);
        var response = new ExportWorkflowStateResponse
        {
            SerializedWorkflowState = new Json
            {
                Text = json
            }
        };

        return response;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.DeserializeAsync(String, CancellationToken)")]
    public override async Task<ImportWorkflowStateResponse> ImportState(ImportWorkflowStateRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var workflowStateSerializer = scope.ServiceProvider.GetRequiredService<IWorkflowStateSerializer>();
        var workflowState = await workflowStateSerializer.DeserializeAsync(request.SerializedWorkflowState.Text);

        _workflowState = workflowState;
        _workflowHost.WorkflowState = workflowState;
        _definitionId = workflowState.DefinitionId;
        _instanceId = workflowState.Id;
        _version = workflowState.DefinitionVersion;
        _workflowHost = await CreateWorkflowHostAsync(workflowState, Context.CancellationToken);

        await SaveWorkflowInstanceCoreAsync(scope.ServiceProvider, workflowState);
        return new ImportWorkflowStateResponse();
    }

    private void ApplySnapshot(Snapshot snapshot) => (_definitionId, _instanceId, _version, _workflowState, _input) = (WorkflowInstanceSnapshot)snapshot.State;

    private async Task SaveSnapshotAsync()
    {
        if (_workflowState.Status == WorkflowStatus.Finished)
            // If the workflow has finished, delete all snapshots.
            await _persistence.DeleteSnapshotsAsync(_persistence.Index);
        else
            // Otherwise, create a new snapshot, automatically deleting the last N snapshots. 
            await _persistence.PersistRollingSnapshotAsync(GetState(), MaxSnapshotsToKeep);
    }

    private object GetState() => new WorkflowInstanceSnapshot(_definitionId, _instanceId, _version, _workflowState, _input?.ToDictionary(x => x.Key, x => x.Value));

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(
        string definitionId,
        VersionOptions versionOptions,
        string? instanceId = null,
        bool isExistingInstance = false,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);

        if (workflow == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflowHostFactory = scope.ServiceProvider.GetRequiredService<IWorkflowHostFactory>();

        if (isExistingInstance)
        {
            var workflowState = await LoadWorkflowStateAsync(instanceId!, cancellationToken);
            return await workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
        }

        return await workflowHostFactory.CreateAsync(workflow, cancellationToken);
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var definitionId = workflowState.DefinitionId;
        var versionOptions = VersionOptions.SpecificVersion(workflowState.DefinitionVersion);
        using var scope = _scopeFactory.CreateScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);

        if (workflow == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflowHostFactory = scope.ServiceProvider.GetRequiredService<IWorkflowHostFactory>();
        return await workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
    }

    private async Task<WorkflowState> LoadWorkflowStateAsync(string instanceId, CancellationToken cancellationToken)
    {
        var workflowInstance = await _workflowInstanceStore.FindAsync(instanceId, cancellationToken) ?? throw new Exception($"Workflow instance {instanceId} not found");
        return _workflowStateMapper.Map(workflowInstance)!;
    }

    /// <summary>
    /// Asynchronously persists the workflow instance.
    /// </summary>
    private void SaveWorkflowInstance(WorkflowState workflowState)
    {
        using var scope = _scopeFactory.CreateScope();
        var saveInstanceTask = SaveWorkflowInstanceCoreAsync(scope.ServiceProvider, workflowState);
        Context.ReenterAfter(saveInstanceTask, () => { });
    }

    private Task SaveWorkflowInstanceCoreAsync(IServiceProvider sp, WorkflowState workflowState)
    {
        var workflowInstance = _workflowStateMapper.Map(workflowState)!;
        var workflowInstanceManager = sp.GetRequiredService<IWorkflowInstanceManager>();
        return workflowInstanceManager.SaveAsync(workflowInstance);
    }
}