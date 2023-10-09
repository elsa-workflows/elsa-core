using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Medallion.Threading;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A default implementation of <see cref="IWorkflowRuntime"/> that relies on persistent storage and distributed locking for workflow execution.
/// </summary>
public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowInstanceManager _workflowInstanceManager;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkHasher _hasher;
    private readonly IBookmarkManager _bookmarkManager;
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;
    private readonly WorkflowStateMapper _workflowStateMapper;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowRuntime(
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowInstanceManager workflowInstanceManager,
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore,
        IBookmarkHasher hasher,
        IBookmarkManager bookmarkManager,
        IDistributedLockProvider distributedLockProvider,
        IWorkflowInstanceFactory workflowInstanceFactory,
        WorkflowStateMapper workflowStateMapper,
        IIdentityGenerator identityGenerator)
    {
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowInstanceStore = workflowInstanceStore;
        _workflowInstanceManager = workflowInstanceManager;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _hasher = hasher;
        _bookmarkManager = bookmarkManager;
        _distributedLockProvider = distributedLockProvider;
        _workflowInstanceFactory = workflowInstanceFactory;
        _workflowStateMapper = workflowStateMapper;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public async Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options)
    {
        var input = options.Input;
        var correlationId = options.CorrelationId;
        var workflowHost = await CreateWorkflowHostAsync(definitionId, options, options.CancellationTokens.SystemCancellationToken);
        var startWorkflowOptions = new StartWorkflowHostOptions(default, correlationId, input, options.TriggerActivityId);
        var canStart = await workflowHost.CanStartWorkflowAsync(startWorkflowOptions, options.CancellationTokens.SystemCancellationToken);
        return new CanStartWorkflowResult(null, canStart);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options)
    {
        var workflowHost = await CreateWorkflowHostAsync(definitionId, options, options.CancellationTokens.SystemCancellationToken);
        return await StartWorkflowAsync(workflowHost, options);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options)
    {
        return await StartWorkflowAsync(definitionId, options);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions options)
    {
        var results = new List<WorkflowExecutionResult>();
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var systemCancellationToken = options.CancellationTokens.SystemCancellationToken;

        // Start new workflows. Notice that this happens in a process-synchronized fashion to avoid multiple instances from being created. 
        var sharedResource = $"{nameof(DefaultWorkflowRuntime)}__StartTriggeredWorkflows__{hash}";
        await using (await _distributedLockProvider.AcquireLockAsync(sharedResource, TimeSpan.FromMinutes(2), systemCancellationToken))
        {
            var filter = new TriggerFilter { Hash = hash };
            var triggers = await _triggerStore.FindManyAsync(filter, systemCancellationToken);

            foreach (var trigger in triggers)
            {
                var definitionId = trigger.WorkflowDefinitionId;

                var startOptions = new StartWorkflowRuntimeOptions(
                    options.CorrelationId,
                    options.Input,
                    VersionOptions.Published,
                    trigger.ActivityId,
                    options.WorkflowInstanceId,
                    options.CancellationTokens);

                var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);

                // If we can't start the workflow, don't try it.
                if (!canStartResult.CanStart)
                    continue;

                var startResult = await StartWorkflowAsync(definitionId, startOptions);
                results.Add(startResult with { TriggeredActivityId = trigger.ActivityId });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeOptions options)
    {
        var applicationCancellationToken = options.CancellationTokens.ApplicationCancellationToken;
        var systemCancellationToken = options.CancellationTokens.SystemCancellationToken;

        await using (await _distributedLockProvider.AcquireLockAsync(workflowInstanceId, TimeSpan.FromMinutes(2), systemCancellationToken))
        {
            var workflowInstance = await _workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = workflowInstanceId }, systemCancellationToken);

            if (workflowInstance == null)
                return null;

            var definitionId = workflowInstance.DefinitionId;
            var version = workflowInstance.Version;

            var workflowDefinition = await _workflowDefinitionService.FindAsync(
                definitionId,
                VersionOptions.SpecificVersion(version),
                systemCancellationToken);

            if (workflowDefinition == null)
            {
                throw new Exception($"The workflow definition {definitionId} version {version} was not found");
            }

            var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, systemCancellationToken);
            var workflowState = _workflowStateMapper.Map(workflowInstance)!;
            var workflowHost = await _workflowHostFactory.CreateAsync(workflow, workflowState, systemCancellationToken);

            var resumeWorkflowOptions = new ResumeWorkflowHostOptions(
                options.CorrelationId,
                options.BookmarkId,
                options.ActivityId,
                options.ActivityNodeId,
                options.ActivityInstanceId,
                options.ActivityHash,
                options.Input,
                options.CancellationTokens);

            await workflowHost.ResumeWorkflowAsync(resumeWorkflowOptions, applicationCancellationToken);

            workflowState = workflowHost.WorkflowState;

            await SaveWorkflowStateAsync(workflowState, systemCancellationToken);

            return new WorkflowExecutionResult(workflowState.Id, workflowState.Status, workflowState.SubStatus, workflowState.Bookmarks, workflowState.Incidents);
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions options)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload, options.ActivityInstanceId);
        var correlationId = options.CorrelationId;
        var workflowInstanceId = options.WorkflowInstanceId;
        var activityInstanceId = options.ActivityInstanceId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId, WorkflowInstanceId = workflowInstanceId, ActivityInstanceId = activityInstanceId };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, options.CancellationTokens.SystemCancellationToken);

        return await ResumeWorkflowsAsync(
            bookmarks,
            new ResumeWorkflowRuntimeOptions(
                correlationId,
                Input: options.Input,
                CancellationTokens: options.CancellationTokens));
    }

    /// <inheritdoc />
    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions options)
    {
        var startedWorkflows = await StartWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var resumedWorkflows = await ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        var results = startedWorkflows.Concat(resumedWorkflows).ToList();
        return new TriggerWorkflowsResult(results);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, IDictionary<string, object>? input = default, CancellationTokens cancellationTokens = default)
    {
        if (match is StartableWorkflowMatch collectedStartableWorkflow)
        {
            var startOptions = new StartWorkflowRuntimeOptions(
                collectedStartableWorkflow.CorrelationId,
                input,
                VersionOptions.Published,
                collectedStartableWorkflow.ActivityId,
                collectedStartableWorkflow.WorkflowInstanceId,
                cancellationTokens);

            var startResult = await StartWorkflowAsync(collectedStartableWorkflow.DefinitionId!, startOptions);
            return startResult with { TriggeredActivityId = collectedStartableWorkflow.ActivityId };
        }

        var collectedResumableWorkflow = (match as ResumableWorkflowMatch)!;
        var runtimeOptions = new ResumeWorkflowRuntimeOptions(collectedResumableWorkflow.CorrelationId, Input: input);

        return (await ResumeWorkflowAsync(match.WorkflowInstanceId, runtimeOptions with { BookmarkId = collectedResumableWorkflow.BookmarkId }))!;
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
        var workflowInstance = await _workflowInstanceStore.FindAsync(workflowInstanceId, cancellationToken);
        return _workflowStateMapper.Map(workflowInstance);
    }

    /// <inheritdoc />
    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowInstance = _workflowStateMapper.Map(workflowState)!;

        await _workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateBookmarksAsync(UpdateBookmarksRequest request, CancellationToken cancellationToken = default)
    {
        var instanceId = request.WorkflowExecutionContext.Id;
        await RemoveBookmarksAsync(instanceId, request.Diff.Removed, cancellationToken);
        await StoreBookmarksAsync(instanceId, request.Diff.Added, request.CorrelationId, cancellationToken);
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

    private async Task<WorkflowExecutionResult> StartWorkflowAsync(IWorkflowHost workflowHost, StartWorkflowRuntimeOptions options)
    {
        var workflowInstanceId = string.IsNullOrEmpty(options.InstanceId) ? _identityGenerator.GenerateId() : options.InstanceId;
        var cancellationTokens = options.CancellationTokens;

        await using (await _distributedLockProvider.AcquireLockAsync(workflowInstanceId, TimeSpan.FromMinutes(2), cancellationTokens.SystemCancellationToken))
        {
            var input = options.Input;
            var correlationId = options.CorrelationId;
            var startWorkflowOptions = new StartWorkflowHostOptions(workflowInstanceId, correlationId, input, options.TriggerActivityId, cancellationTokens);
            await workflowHost.StartWorkflowAsync(startWorkflowOptions, cancellationTokens.ApplicationCancellationToken);
            var workflowState = workflowHost.WorkflowState;

            await SaveWorkflowStateAsync(workflowState, cancellationTokens.SystemCancellationToken);

            return new WorkflowExecutionResult(
                workflowState.Id,
                workflowState.Status,
                workflowState.SubStatus,
                workflowState.Bookmarks,
                workflowState.Incidents,
                default);
        }
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken)
    {
        var versionOptions = options.VersionOptions;
        var host = await CreateWorkflowHostAsync(definitionId, versionOptions, cancellationToken);

        if (host == null)
            throw new Exception("Specified workflow definition and version does not exist");

        return host;
    }

    private async Task<IWorkflowHost?> CreateWorkflowHostAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken)
    {
        return await _workflowHostFactory.CreateAsync(definitionId, versionOptions, cancellationToken);
    }

    private async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeOptions runtimeOptions)
    {
        var resumedWorkflows = new List<WorkflowExecutionResult>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var resumeResult = await ResumeWorkflowAsync(
                workflowInstanceId,
                runtimeOptions with
                {
                    BookmarkId = bookmark.BookmarkId,
                    ActivityInstanceId = bookmark.ActivityInstanceId
                });

            if (resumeResult != null)
                resumedWorkflows.Add(new WorkflowExecutionResult(workflowInstanceId, resumeResult.Status, resumeResult.SubStatus, resumeResult.Bookmarks, resumeResult.Incidents));
        }

        return resumedWorkflows;
    }

    private async Task SaveWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        await _workflowInstanceManager.SaveAsync(workflowState, cancellationToken);
    }

    private async Task StoreBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, string? correlationId, CancellationToken cancellationToken)
    {
        foreach (var bookmark in bookmarks)
        {
            var storedBookmark = new StoredBookmark(bookmark.Id, bookmark.Name, bookmark.Hash, workflowInstanceId, bookmark.CreatedAt, bookmark.ActivityInstanceId, correlationId, bookmark.Payload);
            await _bookmarkStore.SaveAsync(storedBookmark, cancellationToken);
        }
    }

    private async Task RemoveBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var matchingHashes = bookmarks.Select(x => x.Hash).ToList();
        var filter = new BookmarkFilter { Hashes = matchingHashes, WorkflowInstanceId = workflowInstanceId };
        await _bookmarkManager.DeleteManyAsync(filter, cancellationToken);
    }

    private async Task<IEnumerable<WorkflowMatch>> FindStartableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken = default)
    {
        var results = new List<WorkflowMatch>();
        var hash = _hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);

        // Start new workflows. Notice that this happens in a process-synchronized fashion to avoid multiple instances from being created. 
        var sharedResource = $"{nameof(DefaultWorkflowRuntime)}__StartTriggeredWorkflows__{hash}";
        await using (await _distributedLockProvider.AcquireLockAsync(sharedResource, TimeSpan.FromMinutes(10), cancellationToken))
        {
            var filter = new TriggerFilter { Hash = hash };
            var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);

            foreach (var trigger in triggers)
            {
                var definitionId = trigger.WorkflowDefinitionId;
                var startOptions = new StartWorkflowRuntimeOptions(workflowsFilter.Options.CorrelationId, workflowsFilter.Options.Input, VersionOptions.Published, trigger.ActivityId);
                var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions);
                var workflowInstance = await _workflowInstanceFactory.CreateAsync(definitionId, workflowsFilter.Options.CorrelationId, cancellationToken);

                if (canStartResult.CanStart)
                {
                    results.Add(new StartableWorkflowMatch(workflowInstance.Id, workflowInstance, workflowsFilter.Options.CorrelationId, trigger.ActivityId, definitionId, trigger.Payload));
                }
            }
        }

        return results;
    }

    private async Task<IEnumerable<WorkflowMatch>> FindResumableWorkflowsAsync(WorkflowsFilter workflowsFilter, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(workflowsFilter.ActivityTypeName, workflowsFilter.BookmarkPayload);
        var correlationId = workflowsFilter.Options.CorrelationId;
        var workflowInstanceId = workflowsFilter.Options.WorkflowInstanceId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId, WorkflowInstanceId = workflowInstanceId };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, cancellationToken);
        var collectedWorkflows = bookmarks.Select(b => new ResumableWorkflowMatch(b.WorkflowInstanceId, default, correlationId, b.BookmarkId, b.Payload)).ToList();
        return collectedWorkflows;
    }
}