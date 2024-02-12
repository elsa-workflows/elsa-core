using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Mappers;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Proto.Cluster;
using Bookmark = Elsa.Workflows.Models.Bookmark;
using CountRunningWorkflowsRequest = Elsa.Workflows.Runtime.Requests.CountRunningWorkflowsRequest;
using WorkflowStatus = Elsa.Workflows.WorkflowStatus;

namespace Elsa.ProtoActor.Services;

/// <summary>
/// A Proto.Actor implementation of <see cref="IWorkflowRuntime"/>.
/// </summary>
internal class ProtoActorWorkflowRuntime : IWorkflowRuntime
{
    private readonly Cluster _cluster;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IBookmarkHasher _hasher;
    private readonly IBookmarkManager _bookmarkManager;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;
    private readonly WorkflowExecutionResultMapper _workflowExecutionResultMapper;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProtoActorWorkflowRuntime(
        Cluster cluster,
        IWorkflowStateSerializer workflowStateSerializer,
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore,
        IWorkflowInstanceStore workflowInstanceStore,
        IIdentityGenerator identityGenerator,
        IBookmarkHasher hasher,
        IBookmarkManager bookmarkManager,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowInstanceFactory workflowInstanceFactory,
        WorkflowExecutionResultMapper workflowExecutionResultMapper)
    {
        _cluster = cluster;
        _workflowStateSerializer = workflowStateSerializer;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _workflowInstanceStore = workflowInstanceStore;
        _identityGenerator = identityGenerator;
        _hasher = hasher;
        _bookmarkManager = bookmarkManager;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowInstanceFactory = workflowInstanceFactory;
        _workflowExecutionResultMapper = workflowExecutionResultMapper;
    }

    /// <inheritdoc />
    public async Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions? options  = default)
    {
        var versionOptions = options?.VersionOptions;
        var correlationId = options?.CorrelationId;
        var input = options?.Input;
        var workflowInstanceId = _identityGenerator.GenerateId();

        var request = new StartWorkflowRequest
        {
            DefinitionId = definitionId,
            InstanceId = workflowInstanceId,
            VersionOptions = versionOptions.ToString(),
            CorrelationId = correlationId.EmptyIfNull(),
            Input = input?.SerializeInput(),
            Properties = options?.Properties?.SerializeProperties(),
            TriggerActivityId = options?.TriggerActivityId.EmptyIfNull(),
        };

        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var response = await client.CanStart(request, options?.CancellationTokens.SystemCancellationToken ?? default);

        return new CanStartWorkflowResult(workflowInstanceId, response!.CanStart);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions? options = default)
    {
        // Load the workflow definition.
        var versionOptions = options?.VersionOptions ?? VersionOptions.Published;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, options?.CancellationTokens.SystemCancellationToken ?? default);

        if (workflowDefinition == null)
            return null;

        return await StartWorkflowAsync(definitionId, options);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions? options = default)
    {
        var versionOptions = options?.VersionOptions;
        var correlationId = options?.CorrelationId;
        var workflowInstanceId = options?.InstanceId ?? _identityGenerator.GenerateId();
        var input = options?.Input;

        var request = new StartWorkflowRequest
        {
            DefinitionId = definitionId,
            InstanceId = workflowInstanceId,
            VersionOptions = versionOptions.ToString(),
            CorrelationId = correlationId.WithDefault(""),
            Input = input?.SerializeInput(),
            Properties = options?.Properties?.SerializeProperties(),
            TriggerActivityId = options?.TriggerActivityId.WithDefault("")
        };

        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var response = await client.Start(request, options?.CancellationTokens.SystemCancellationToken ?? default);

        return _workflowExecutionResultMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var filter = new TriggerFilter { Hash = hash };
        var systemCancellationToken = options?.CancellationTokens.SystemCancellationToken ?? default;
        var triggers = await _triggerStore.FindManyAsync(filter, systemCancellationToken);
        var results = new List<WorkflowExecutionResult>();

        foreach (var trigger in triggers)
        {
            var definitionId = trigger.WorkflowDefinitionId;

            var startOptions = new StartWorkflowRuntimeOptions
            {
                CorrelationId = options?.CorrelationId,
                Input = options?.Input,
                Properties = options?.Properties,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = trigger.ActivityId,
                InstanceId = options?.WorkflowInstanceId,
                CancellationTokens = options?.CancellationTokens ?? default
            };

            var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);

            // If we can't start the workflow, don't try it.
            if (!canStartResult.CanStart)
                continue;

            var startResult = await StartWorkflowAsync(definitionId, startOptions);
            results.Add(startResult);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeOptions? options = default)
    {
        var request = new ResumeWorkflowRequest
        {
            InstanceId = workflowInstanceId,
            CorrelationId = options?.CorrelationId.EmptyIfNull(),
            BookmarkId = options?.BookmarkId.EmptyIfNull(),
            ActivityId = options?.ActivityId.EmptyIfNull(),
            Input = options?.Input?.SerializeInput(),
            Properties = options?.Properties?.SerializeProperties(),
        };

        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var response = await client.Resume(request, options?.CancellationTokens.SystemCancellationToken ?? default);

        return _workflowExecutionResultMapper.Map(response!);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options  = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload, options?.ActivityInstanceId);
        var correlationId = options?.CorrelationId;
        var workflowInstanceId = options?.WorkflowInstanceId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId, WorkflowInstanceId = workflowInstanceId };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, options?.CancellationTokens.SystemCancellationToken ?? default);

        return await ResumeWorkflowsAsync(
            bookmarks,
            new ResumeWorkflowRuntimeOptions
            {
                CorrelationId = correlationId,
                Input = options?.Input,
                Properties = options?.Properties,
                CancellationTokens = options?.CancellationTokens ?? default
            }
        );
    }

    /// <inheritdoc />
    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default)
    {
        var startedWorkflows = await StartWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var resumedWorkflows = await ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var results = startedWorkflows.Concat(resumedWorkflows).ToList();

        return new TriggerWorkflowsResult(results);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowOptions? options = default)
    {
        if (match is StartableWorkflowMatch collectedStartableWorkflow)
        {
            var startOptions = new StartWorkflowRuntimeOptions
            {
                CorrelationId = collectedStartableWorkflow.CorrelationId,
                Input = options?.Input,
                Properties = options?.Properties,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = collectedStartableWorkflow.ActivityId,
                InstanceId = collectedStartableWorkflow.WorkflowInstanceId,
                CancellationTokens = options?.CancellationTokens ?? default
            };
            return await StartWorkflowAsync(collectedStartableWorkflow.DefinitionId!, startOptions);
        }

        var collectedResumableWorkflow = (match as ResumableWorkflowMatch)!;
        var runtimeOptions = new ResumeWorkflowRuntimeOptions
        {
            CorrelationId = collectedResumableWorkflow.CorrelationId,
            Input = options?.Input,
            Properties = options?.Properties,
            BookmarkId = collectedResumableWorkflow.BookmarkId,
            CancellationTokens = options?.CancellationTokens ?? default
        };
        var result = await ResumeWorkflowAsync(match.WorkflowInstanceId, runtimeOptions);

        return result!;
    }

    /// <inheritdoc />
    public async Task CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        await client.Cancel(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default)
    {
        var startableWorkflows = await FindStartableWorkflowsAsync(filter, cancellationToken);
        var resumableWorkflows = await FindResumableWorkflowsAsync(filter, cancellationToken);
        var results = startableWorkflows.Concat(resumableWorkflows).ToList();
        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var client = _cluster.GetNamedWorkflowGrain(workflowInstanceId);
        var response = await client.ExportState(new ExportWorkflowStateRequest(), cancellationToken);
        var json = response!.SerializedWorkflowState.Text;
        var workflowState = await _workflowStateSerializer.DeserializeAsync(json, cancellationToken);
        return workflowState;
    }

    /// <inheritdoc />
    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var client = _cluster.GetNamedWorkflowGrain(workflowState.Id);
        var json = await _workflowStateSerializer.SerializeAsync(workflowState, cancellationToken);

        var request = new ImportWorkflowStateRequest
        {
            SerializedWorkflowState = new Json
            {
                Text = json
            }
        };

        await client.ImportState(request, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default)
    {
        await _bookmarkStore.SaveAsync(bookmark, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountRunningWorkflowsAsync(CountRunningWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = request.DefinitionId,
            Version = request.Version,
            CorrelationId = request.CorrelationId,
            WorkflowStatus = WorkflowStatus.Running
        };
        return await _workflowInstanceStore.CountAsync(filter, cancellationToken);
    }

    private async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeOptions runtimeOptions)
    {
        var resumedWorkflows = new List<WorkflowExecutionResult>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var newRuntimeOptions = new ResumeWorkflowRuntimeOptions
            {
                CorrelationId = runtimeOptions.CorrelationId,
                Input = runtimeOptions.Input,
                Properties = runtimeOptions.Properties,
                BookmarkId = bookmark.BookmarkId,
                ActivityId = runtimeOptions.ActivityId,
                ActivityNodeId = runtimeOptions.ActivityNodeId,
                ActivityInstanceId = runtimeOptions.ActivityInstanceId,
                ActivityHash = runtimeOptions.ActivityHash,
                CancellationTokens = runtimeOptions.CancellationTokens
            };

            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, newRuntimeOptions);
            resumedWorkflows.Add(resumeResult!);
        }

        return resumedWorkflows;
    }

    private async Task<IEnumerable<WorkflowMatch>> FindStartableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);
        var filter = new TriggerFilter { Hash = hash };
        var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);
        var results = new List<WorkflowMatch>();

        foreach (var trigger in triggers)
        {
            var definitionId = trigger.WorkflowDefinitionId;

            var startOptions = new StartWorkflowRuntimeOptions
            {
                CorrelationId = workflowsFilter.Options.CorrelationId,
                Input = workflowsFilter.Options.Input,
                Properties = workflowsFilter.Options.Properties,
                VersionOptions = VersionOptions.Published,
                TriggerActivityId = trigger.ActivityId,
                CancellationTokens = cancellationToken
            };

            var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);
            var workflowInstance = await _workflowInstanceFactory.CreateAsync(definitionId, workflowsFilter.Options.CorrelationId, cancellationToken);

            if (canStartResult.CanStart)
                results.Add(new StartableWorkflowMatch(workflowInstance.Id, workflowInstance, workflowsFilter.Options.CorrelationId, trigger.ActivityId, definitionId, trigger.Payload));
        }

        return results;
    }

    private async Task<IEnumerable<WorkflowMatch>> FindResumableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);
        var correlationId = workflowsFilter.Options.CorrelationId;
        var workflowInstanceId = workflowsFilter.Options.WorkflowInstanceId;
        var activityInstanceId = workflowsFilter.Options.ActivityInstanceId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId, WorkflowInstanceId = workflowInstanceId, ActivityInstanceId = activityInstanceId };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, cancellationToken);
        var collectedWorkflows = bookmarks.Select(b => new ResumableWorkflowMatch(b.WorkflowInstanceId, default, correlationId, b.BookmarkId, b.Payload)).ToList();
        return collectedWorkflows;
    }
}