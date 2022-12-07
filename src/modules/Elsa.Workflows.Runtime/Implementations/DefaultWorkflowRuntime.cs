using Elsa.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Medallion.Threading;

namespace Elsa.Workflows.Runtime.Implementations;

public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkHasher _hasher;
    private readonly IDistributedLockProvider _distributedLockProvider;

    public DefaultWorkflowRuntime(
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowStateStore workflowStateStore,
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore,
        IBookmarkHasher hasher,
        IDistributedLockProvider distributedLockProvider)
    {
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowStateStore = workflowStateStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _hasher = hasher;
        _distributedLockProvider = distributedLockProvider;
    }

    public async Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var input = options.Input;
        var correlationId = options.CorrelationId;
        var versionOptions = options.VersionOptions;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowHost = await _workflowHostFactory.CreateAsync(workflow, cancellationToken);

        var startWorkflowOptions = new StartWorkflowHostOptions(default, correlationId, input, options.TriggerActivityId);
        await workflowHost.StartWorkflowAsync(startWorkflowOptions, cancellationToken);
        var workflowState = workflowHost.WorkflowState;

        await SaveWorkflowStateAsync(workflowState, cancellationToken);

        return new StartWorkflowResult(workflowState.Id, workflowHost.WorkflowState.Bookmarks);
    }

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

    public async Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var correlationId = options.CorrelationId;
        var bookmarks = correlationId == null ? await _bookmarkStore.FindByHashAsync(hash, cancellationToken) : await _bookmarkStore.FindByCorrelationAndHashAsync(correlationId, hash, cancellationToken);
        return await ResumeWorkflowsAsync(bookmarks, options, cancellationToken);
    }

    public async Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeOptions runtimeOptions, CancellationToken cancellationToken = default)
    {
        var resumedWorkflows = new List<ResumedWorkflow>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var resumeOptions = new ResumeWorkflowRuntimeOptions(runtimeOptions.CorrelationId, bookmark.BookmarkId, Input: runtimeOptions.Input);
            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, resumeOptions, cancellationToken);

            resumedWorkflows.Add(new ResumedWorkflow(workflowInstanceId, resumeResult.Bookmarks));
        }

        return resumedWorkflows;
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(
        string activityTypeName,
        object bookmarkPayload,
        TriggerWorkflowsRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        var triggeredWorkflows = new List<TriggeredWorkflow>();
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);

        // Start new workflows.
        var triggers = await _triggerStore.FindAsync(hash, cancellationToken);

        foreach (var trigger in triggers)
        {
            var startResult = await StartWorkflowAsync(
                trigger.WorkflowDefinitionId,
                new StartWorkflowRuntimeOptions(options.CorrelationId, options.Input, VersionOptions.Published, trigger.ActivityId),
                cancellationToken);

            triggeredWorkflows.Add(new TriggeredWorkflow(startResult.InstanceId, startResult.Bookmarks));
        }

        // Resume bookmarks.
        var bookmarks = (await _bookmarkStore.FindByHashAsync(hash, cancellationToken)).ToList();
        var resumedWorkflows = await ResumeWorkflowsAsync(bookmarks, new ResumeWorkflowRuntimeOptions(options.CorrelationId, Input: options.Input), cancellationToken);

        triggeredWorkflows.AddRange(resumedWorkflows.Select(x => new TriggeredWorkflow(x.InstanceId, x.Bookmarks)));
        return new TriggerWorkflowsResult(triggeredWorkflows);
    }

    public async Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        return await _workflowStateStore.LoadAsync(workflowInstanceId, cancellationToken);
    }

    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);
    }

    public async Task UpdateBookmarksAsync(UpdateBookmarksContext context, CancellationToken cancellationToken = default)
    {
        await RemoveBookmarksAsync(context.InstanceId, context.Diff.Removed, cancellationToken);
        await StoreBookmarksAsync(context.InstanceId, context.Diff.Added, context.CorrelationId, cancellationToken);
    }

    private async Task SaveWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken) =>
        await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);

    private async Task StoreBookmarksAsync(string workflowInstanceId, ICollection<Bookmark> bookmarks, string? correlationId, CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => (x.Name, x.Hash));

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var key = groupedBookmark.Key;
            var bookmarkIds = groupedBookmark.Select(x => x.Id).ToList();
            await _bookmarkStore.SaveAsync(key.Name, key.Hash, workflowInstanceId, bookmarkIds, correlationId, cancellationToken);
        }
    }

    private async Task RemoveBookmarksAsync(
        string workflowInstanceId,
        IEnumerable<Bookmark> bookmarks,
        CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => x.Hash);

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var key = groupedBookmark.Key;
            await _bookmarkStore.DeleteAsync(key, workflowInstanceId, cancellationToken);
        }
    }
}