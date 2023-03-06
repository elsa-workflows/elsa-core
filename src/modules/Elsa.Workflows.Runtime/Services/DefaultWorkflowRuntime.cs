using Elsa.Common.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Medallion.Threading;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A default implementation of <see cref="IWorkflowRuntime"/> that relies on persistent storage and distributed locking for workflow execution.
/// </summary>
public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkHasher _hasher;
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowRuntime(
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowStateStore workflowStateStore,
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore,
        IBookmarkHasher hasher,
        IDistributedLockProvider distributedLockProvider,
        IWorkflowInstanceFactory workflowInstanceFactory)
    {
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowStateStore = workflowStateStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _hasher = hasher;
        _distributedLockProvider = distributedLockProvider;
        _workflowInstanceFactory = workflowInstanceFactory;
    }

    /// <inheritdoc />
    public async Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken)
    {
        var input = options.Input;
        var correlationId = options.CorrelationId;
        var workflowHost = await CreateWorkflowHostAsync(definitionId, options, cancellationToken);
        var startWorkflowOptions = new StartWorkflowHostOptions(default, correlationId, input, options.TriggerActivityId);
        var canStart = await workflowHost.CanStartWorkflowAsync(startWorkflowOptions, cancellationToken);
        return new CanStartWorkflowResult(null, canStart);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var input = options.Input;
        var correlationId = options.CorrelationId;
        var workflowHost = await CreateWorkflowHostAsync(definitionId, options, cancellationToken);
        var startWorkflowOptions = new StartWorkflowHostOptions(options.InstanceId, correlationId, input, options.TriggerActivityId);
        await workflowHost.StartWorkflowAsync(startWorkflowOptions, cancellationToken);
        var workflowState = workflowHost.WorkflowState;

        await SaveWorkflowStateAsync(workflowState, cancellationToken);

        return new WorkflowExecutionResult(workflowState.Id, workflowState.Bookmarks);
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(
        string activityTypeName,
        object bookmarkPayload,
        TriggerWorkflowsRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        var results = new List<WorkflowExecutionResult>();
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);

        // Start new workflows. Notice that this happens in a process-synchronized fashion to avoid multiple instances from being created. 
        var sharedResource = $"{nameof(DefaultWorkflowRuntime)}__StartTriggeredWorkflows__{hash}";
        await using (await _distributedLockProvider.AcquireLockAsync(sharedResource, TimeSpan.FromMinutes(10), cancellationToken))
        {
            var filter = new TriggerFilter { Hash = hash };
            var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);

            foreach (var trigger in triggers)
            {
                var definitionId = trigger.WorkflowDefinitionId;
                var startOptions = new StartWorkflowRuntimeOptions(options.CorrelationId, options.Input, VersionOptions.Published, trigger.ActivityId);
                var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions, cancellationToken);

                // If we can't start the workflow, don't try it.
                if (!canStartResult.CanStart)
                    continue;

                var startResult = await StartWorkflowAsync(definitionId, startOptions, cancellationToken);
                results.Add(new WorkflowExecutionResult(startResult.InstanceId, startResult.Bookmarks, trigger.ActivityId));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        await using (await _distributedLockProvider.AcquireLockAsync(workflowInstanceId, TimeSpan.FromMinutes(1), cancellationToken))
        {
            var workflowState = await _workflowStateStore.LoadAsync(workflowInstanceId, cancellationToken);

            if (workflowState == null)
                throw new Exception($"Workflow instance {workflowInstanceId} not found");

            var definitionId = workflowState.DefinitionId;
            var version = workflowState.DefinitionVersion;

            var workflowDefinition = await _workflowDefinitionService.FindAsync(
                definitionId,
                VersionOptions.SpecificVersion(version),
                cancellationToken);

            if (workflowDefinition == null)
                throw new Exception("Specified workflow definition and version does not exist");

            var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
            var workflowHost = await _workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
            var resumeWorkflowOptions = new ResumeWorkflowHostOptions(options.CorrelationId, options.BookmarkId, options.ActivityId, options.Input);

            await workflowHost.ResumeWorkflowAsync(resumeWorkflowOptions, cancellationToken);

            workflowState = workflowHost.WorkflowState;

            await SaveWorkflowStateAsync(workflowState, cancellationToken);

            return new ResumeWorkflowResult(workflowState.Bookmarks);
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var correlationId = options.CorrelationId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, cancellationToken);
        return await ResumeWorkflowsAsync(bookmarks, new ResumeWorkflowRuntimeOptions(options.CorrelationId, Input: options.Input), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var startedWorkflows = await StartWorkflowsAsync(activityTypeName, bookmarkPayload, options, cancellationToken);
        var resumedWorkflows = await ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options, cancellationToken);
        var results = startedWorkflows.Concat(resumedWorkflows).ToList();
        return new TriggerWorkflowsResult(results);
    }
    /*
    public async Task<RunWorkflowResult> ExecutePendingWorkflowAsync(CollectedWorkflow collectedWorkflow, WorkflowInput? input = default, CancellationToken cancellationToken = default)
    {

    }
    */
    public async Task<IEnumerable<CollectedWorkflow>> FindWorkflowsAsync(WorkflowsQuery workflowsQuery, CancellationToken cancellationToken = default)
    {
        var startableWorkflows = await CollectStartableWorkflowsAsync(workflowsQuery, cancellationToken);
        var resumableWorkflows = await CollectResumableWorkflowsAsync(workflowsQuery, cancellationToken);
        var results = startableWorkflows.Concat(resumableWorkflows).ToList();
        return results;
    }

    /// <inheritdoc />
    public async Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => await _workflowStateStore.LoadAsync(workflowInstanceId, cancellationToken);

    /// <inheritdoc />
    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default) => await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);

    /// <inheritdoc />
    public async Task UpdateBookmarksAsync(UpdateBookmarksContext context, CancellationToken cancellationToken = default)
    {
        await RemoveBookmarksAsync(context.InstanceId, context.Diff.Removed, cancellationToken);
        await StoreBookmarksAsync(context.InstanceId, context.Diff.Added, context.CorrelationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountRunningWorkflowsAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default) => await _workflowStateStore.CountAsync(args, cancellationToken);

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken)
    {
        var versionOptions = options.VersionOptions;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        return await _workflowHostFactory.CreateAsync(workflow, cancellationToken);
    }

    private async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeOptions runtimeOptions, CancellationToken cancellationToken = default)
    {
        var resumedWorkflows = new List<WorkflowExecutionResult>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var resumeOptions = new ResumeWorkflowRuntimeOptions(runtimeOptions.CorrelationId, bookmark.BookmarkId, Input: runtimeOptions.Input);
            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, resumeOptions, cancellationToken);

            resumedWorkflows.Add(new WorkflowExecutionResult(workflowInstanceId, resumeResult.Bookmarks));
        }

        return resumedWorkflows;
    }

    private async Task SaveWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken) =>
        await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);

    private async Task StoreBookmarksAsync(string workflowInstanceId, ICollection<Bookmark> bookmarks, string? correlationId, CancellationToken cancellationToken)
    {
        foreach (var bookmark in bookmarks)
        {
            var storedBookmark = new StoredBookmark(bookmark.Name, bookmark.Hash, workflowInstanceId, bookmark.Id, correlationId, bookmark.Data);
            await _bookmarkStore.SaveAsync(storedBookmark, cancellationToken);
        }
    }

    private async Task RemoveBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        foreach (var bookmark in bookmarks)
        {
            var filter = new BookmarkFilter { Hash = bookmark.Hash, WorkflowInstanceId = workflowInstanceId };
            await _bookmarkStore.DeleteAsync(filter, cancellationToken);
        }
    }

    private async Task<ICollection<CollectedWorkflow>> CollectStartableWorkflowsAsync(
        WorkflowsQuery workflowsQuery,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CollectedWorkflow>();
        var hash = _hasher.Hash(workflowsQuery.ActivityTypeName, workflowsQuery.BookmarkPayload);

        // Start new workflows. Notice that this happens in a process-synchronized fashion to avoid multiple instances from being created. 
        var sharedResource = $"{nameof(DefaultWorkflowRuntime)}__StartTriggeredWorkflows__{hash}";
        await using (await _distributedLockProvider.AcquireLockAsync(sharedResource, TimeSpan.FromMinutes(10), cancellationToken))
        {
            var filter = new TriggerFilter { Hash = hash };
            var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);

            foreach (var trigger in triggers)
            {
                var definitionId = trigger.WorkflowDefinitionId;
                var startOptions = new StartWorkflowRuntimeOptions(workflowsQuery.Options.CorrelationId, workflowsQuery.Options.Input, VersionOptions.Published, trigger.ActivityId);
                var canStartResult = await CanStartWorkflowAsync(definitionId, startOptions, cancellationToken);

                var workflowInstance = await _workflowInstanceFactory.CreateAsync(definitionId, workflowsQuery.Options.CorrelationId, cancellationToken);

                if (canStartResult.CanStart)
                {
                    results.Add(new CollectedWorkflow(workflowInstance.Id, workflowInstance, trigger.ActivityId));
                }
            }
        }

        return results;
    }

    private async Task<ICollection<CollectedWorkflow>> CollectResumableWorkflowsAsync(WorkflowsQuery workflowsQuery, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(workflowsQuery.ActivityTypeName, workflowsQuery.BookmarkPayload);
        var correlationId = workflowsQuery.Options.CorrelationId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId };
        var bookmarks = await _bookmarkStore.FindManyAsync(filter, cancellationToken);

        var collectedWorkflows = bookmarks.Select(b => new CollectedWorkflow(b.WorkflowInstanceId, default, default)).ToList();
        return collectedWorkflows;
    }
}