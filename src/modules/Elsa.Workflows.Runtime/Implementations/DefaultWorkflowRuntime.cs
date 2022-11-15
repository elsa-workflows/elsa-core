using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Implementations;

public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkHasher _hasher;
    private readonly IEventPublisher _eventPublisher;

    public DefaultWorkflowRuntime(
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowStateStore workflowStateStore,
        ITriggerStore triggerStore,
        IBookmarkStore bookmarkStore,
        IBookmarkHasher hasher,
        IEventPublisher eventPublisher)
    {
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowStateStore = workflowStateStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _hasher = hasher;
        _eventPublisher = eventPublisher;
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

        var startWorkflowOptions = new StartWorkflowHostOptions(default, correlationId, input);
        await workflowHost.StartWorkflowAsync(startWorkflowOptions, cancellationToken);
        var workflowState = workflowHost.WorkflowState;

        await SaveWorkflowStateAsync(workflowState, cancellationToken);
        await UpdateBookmarksAsync(workflowState, new List<Bookmark>(), workflowState.Bookmarks, cancellationToken);

        return new StartWorkflowResult(workflowState.Id, workflowHost.WorkflowState.Bookmarks);
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var workflowState = await _workflowStateStore.LoadAsync(instanceId, cancellationToken);

        if (workflowState == null)
            throw new Exception($"Workflow instance {instanceId} not found");

        var definitionId = workflowState.DefinitionId;
        var version = workflowState.DefinitionVersion;

        var workflowDefinition = await _workflowDefinitionService.FindAsync(
            definitionId,
            VersionOptions.SpecificVersion(version),
            cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var input = options.Input;

        var existingStoredBookmarks = await _bookmarkStore.FindByWorkflowInstanceAsync(workflowState.Id, cancellationToken)
            .AsTask()
            .ToList();

        var existingBookmarks = existingStoredBookmarks
            .Select(storedBookmark => workflowState.Bookmarks.FirstOrDefault(bookmark => bookmark.Id == storedBookmark.BookmarkId))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowHost = await _workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
        var resumeWorkflowOptions = new ResumeWorkflowHostOptions(input);

        await workflowHost.ResumeWorkflowAsync(bookmarkId, resumeWorkflowOptions, cancellationToken);
        workflowState = workflowHost.WorkflowState;

        await SaveWorkflowStateAsync(workflowState, cancellationToken);
        await UpdateBookmarksAsync(workflowState, existingBookmarks, workflowState.Bookmarks, cancellationToken);

        return new ResumeWorkflowResult(workflowState.Bookmarks);
    }
    
    public async Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var bookmarks = await _bookmarkStore.FindByHashAsync(hash, cancellationToken);
        return await ResumeWorkflowsAsync(bookmarks, options, cancellationToken);
    }
    
    public async Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeOptions runtimeOptions, CancellationToken cancellationToken = default)
    {
        var resumedWorkflows = new List<ResumedWorkflow>();
        
        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var resumeResult = await ResumeWorkflowAsync(
                workflowInstanceId,
                bookmark.BookmarkId,
                new ResumeWorkflowRuntimeOptions(runtimeOptions.Input),
                cancellationToken);

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
                new StartWorkflowRuntimeOptions(options.CorrelationId, options.Input, VersionOptions.Published),
                cancellationToken);

            triggeredWorkflows.Add(new TriggeredWorkflow(startResult.InstanceId, startResult.Bookmarks));
        }

        // Resume bookmarks.
        var bookmarks = (await _bookmarkStore.FindByHashAsync(hash, cancellationToken)).ToList();
        var resumedWorkflows = await ResumeWorkflowsAsync(bookmarks, new ResumeWorkflowRuntimeOptions(options.Input), cancellationToken);
        
        triggeredWorkflows.AddRange(resumedWorkflows.Select(x => new TriggeredWorkflow(x.InstanceId, x.Bookmarks)));
        return new TriggerWorkflowsResult(triggeredWorkflows);
    }

    public async Task<WorkflowState?> ExportWorkflowStateAsync(string instanceId,
        CancellationToken cancellationToken = default)
    {
        return await _workflowStateStore.LoadAsync(instanceId, cancellationToken);
    }

    public async Task ImportWorkflowStateAsync(WorkflowState workflowState,
        CancellationToken cancellationToken = default)
    {
        await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);
    }

    private async Task SaveWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken) =>
        await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);

    private async Task UpdateBookmarksAsync(WorkflowState workflowState, ICollection<Bookmark> previousBookmarks,
        ICollection<Bookmark> newBookmarks, CancellationToken cancellationToken)
    {
        await RemoveBookmarksAsync(workflowState.Id, previousBookmarks, cancellationToken);
        await StoreBookmarksAsync(workflowState.Id, newBookmarks, cancellationToken);
        await PublishChangedBookmarksAsync(workflowState, previousBookmarks, newBookmarks, cancellationToken);
    }

    private async Task StoreBookmarksAsync(string workflowInstanceId, ICollection<Bookmark> bookmarks,
        CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => (x.Name, x.Hash));

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var key = groupedBookmark.Key;
            var bookmarkIds = groupedBookmark.Select(x => x.Id).ToList();
            await _bookmarkStore.SaveAsync(key.Name, key.Hash, workflowInstanceId, bookmarkIds, cancellationToken);
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

    private async Task PublishChangedBookmarksAsync(WorkflowState workflowState,
        ICollection<Bookmark> originalBookmarks, ICollection<Bookmark> updatedBookmarks,
        CancellationToken cancellationToken)
    {
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        var removedBookmarks = diff.Removed;
        var createdBookmarks = diff.Added;
        await _eventPublisher.PublishAsync(
            new WorkflowBookmarksIndexed(
                new IndexedWorkflowBookmarks(workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }
}