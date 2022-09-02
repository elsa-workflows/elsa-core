using Elsa.Common.Services;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Implementations;

public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IBookmarkManager _bookmarkManager;
    private readonly IHasher _hasher;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISystemClock _systemClock;

    public DefaultWorkflowRuntime(
        IWorkflowRunner workflowRunner,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowInstanceStore workflowInstanceStore,
        IBookmarkManager bookmarkManager,
        IHasher hasher,
        IEventPublisher eventPublisher,
        ISystemClock systemClock)
    {
        _workflowRunner = workflowRunner;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowInstanceStore = workflowInstanceStore;
        _bookmarkManager = bookmarkManager;
        _hasher = hasher;
        _eventPublisher = eventPublisher;
        _systemClock = systemClock;
    }

    public async Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var input = options.Input;
        var versionOptions = options.VersionOptions;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowResult = await _workflowRunner.RunAsync(workflow, input, cancellationToken);
        var workflowState = workflowResult.WorkflowState;
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        var workflowInstance = await SaveWorkflowInstanceAsync(workflowDefinition, workflowState, cancellationToken);
        await UpdateBookmarksAsync(workflowInstance, new List<Bookmark>(), workflowResult.Bookmarks, cancellationToken);

        return new StartWorkflowResult(workflowInstance.Id);
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await _workflowInstanceStore.FindByIdAsync(instanceId, cancellationToken);

        if (workflowInstance == null)
            throw new Exception($"Workflow instance {instanceId} not found");

        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var input = options.Input;

        var existingBookmarks = await _bookmarkManager.FindManyByWorkflowInstanceIdAsync(workflowInstance.Id, cancellationToken).ToList();
        var bookmark = existingBookmarks.FirstOrDefault(x => x.Id == bookmarkId);

        if (bookmark == null)
            throw new Exception("Bookmark not found");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowState = workflowInstance.WorkflowState;
        var workflowResult = await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        workflowInstance = await SaveWorkflowInstanceAsync(workflowDefinition, workflowState, cancellationToken);
        await UpdateBookmarksAsync(workflowInstance, existingBookmarks, workflowResult.Bookmarks, cancellationToken);

        return new ResumeWorkflowResult();
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string bookmarkName, object bookmarkPayload, TriggerWorkflowsOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(bookmarkPayload);
        var bookmarks = await _bookmarkManager.FindManyByHashAsync(bookmarkName, hash, cancellationToken);

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, bookmark.Id, new ResumeWorkflowOptions(options.Input), cancellationToken);    
        }

        return new TriggerWorkflowsResult();
    }

    private async Task<WorkflowInstance> SaveWorkflowInstanceAsync(WorkflowDefinition workflowDefinition, WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = FromWorkflowState(workflowState, workflowDefinition);
        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }

    private WorkflowInstance FromWorkflowState(WorkflowState workflowState, WorkflowDefinition workflowDefinition)
    {
        var workflowInstance = new WorkflowInstance
        {
            Id = workflowState.Id,
            DefinitionId = workflowDefinition.DefinitionId,
            DefinitionVersionId = workflowDefinition.Id,
            Version = workflowDefinition.Version,
            WorkflowState = workflowState,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            CorrelationId = workflowState.CorrelationId,
            Name = null,
        };

        // Update timestamps.
        var now = _systemClock.UtcNow;

        if (workflowInstance.Status == WorkflowStatus.Finished)
        {
            switch (workflowInstance.SubStatus)
            {
                case WorkflowSubStatus.Cancelled:
                    workflowInstance.CancelledAt = now;
                    break;
                case WorkflowSubStatus.Faulted:
                    workflowInstance.FaultedAt = now;
                    break;
                case WorkflowSubStatus.Finished:
                    workflowInstance.FinishedAt = now;
                    break;
            }
        }

        return workflowInstance;
    }

    private async Task UpdateBookmarksAsync(WorkflowInstance workflowInstance, ICollection<Bookmark> previousBookmarks, ICollection<Bookmark> newBookmarks, CancellationToken cancellationToken)
    {
        await RemoveBookmarksAsync(previousBookmarks, cancellationToken);
        await StoreBookmarksAsync(workflowInstance, newBookmarks, cancellationToken);
        await PublishChangedBookmarksAsync(workflowInstance.WorkflowState, previousBookmarks, newBookmarks, cancellationToken);
    }

    private async Task StoreBookmarksAsync(WorkflowInstance workflowInstance, ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        await _bookmarkManager.SaveAsync(workflowInstance, bookmarks, cancellationToken);
    }

    private async Task RemoveBookmarksAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        await _bookmarkManager.DeleteAsync(bookmarks, cancellationToken);
    }

    private async Task PublishChangedBookmarksAsync(WorkflowState workflowState, ICollection<Bookmark> originalBookmarks, ICollection<Bookmark> updatedBookmarks, CancellationToken cancellationToken)
    {
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        var removedBookmarks = diff.Removed;
        var createdBookmarks = diff.Added;
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }
}