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
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IHasher _hasher;
    private readonly IEventPublisher _eventPublisher;

    public DefaultWorkflowRuntime(
        IWorkflowRunner workflowRunner,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowStateStore workflowStateStore,
        IBookmarkStore bookmarkStore,
        IHasher hasher,
        IEventPublisher eventPublisher)
    {
        _workflowRunner = workflowRunner;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowStateStore = workflowStateStore;
        _bookmarkStore = bookmarkStore;
        _hasher = hasher;
        _eventPublisher = eventPublisher;
    }

    public async Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var input = options.Input;
        var versionOptions = options.VersionOptions;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);

        await _eventPublisher.PublishAsync(new WorkflowExecuting(workflow), cancellationToken);
        
        var workflowResult = await _workflowRunner.RunAsync(workflow, input, cancellationToken);
        var workflowState = workflowResult.WorkflowState;
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        await SaveWorkflowStateAsync(workflowState, cancellationToken);
        await UpdateBookmarksAsync(workflowState, new List<Bookmark>(), workflowResult.WorkflowState.Bookmarks, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowExecuted(workflow, workflowState), cancellationToken);

        return new StartWorkflowResult(workflowState.Id);
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var workflowState = await _workflowStateStore.LoadAsync(instanceId, cancellationToken);

        if (workflowState == null)
            throw new Exception($"Workflow instance {instanceId} not found");

        var definitionId = workflowState.DefinitionId;
        var version = workflowState.DefinitionVersion;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var input = options.Input;
        var existingStoredBookmarks = await _bookmarkStore.LoadAsync(workflowState.Id, cancellationToken).AsTask().ToList();

        var existingBookmarks = existingStoredBookmarks
            .Select(storedBookmark => workflowState.Bookmarks.FirstOrDefault(bookmark => bookmark.Id == storedBookmark.BookmarkId))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        
        await _eventPublisher.PublishAsync(new WorkflowExecuting(workflow), cancellationToken);
        
        var workflowResult = await _workflowRunner.RunAsync(workflow, workflowState, bookmarkId, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        await SaveWorkflowStateAsync(workflowState, cancellationToken);
        await UpdateBookmarksAsync(workflowState, existingBookmarks, workflowResult.WorkflowState.Bookmarks, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowExecuted(workflow, workflowState), cancellationToken);

        return new ResumeWorkflowResult();
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(bookmarkPayload);
        var bookmarks = await _bookmarkStore.LoadAsync(activityTypeName, hash, cancellationToken);

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, bookmark.BookmarkId, new ResumeWorkflowOptions(options.Input), cancellationToken);
        }

        return new TriggerWorkflowsResult();
    }

    private async Task SaveWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken) => await _workflowStateStore.SaveAsync(workflowState.Id, workflowState, cancellationToken);

    private async Task UpdateBookmarksAsync(WorkflowState workflowState, ICollection<Bookmark> previousBookmarks, ICollection<Bookmark> newBookmarks, CancellationToken cancellationToken)
    {
        await RemoveBookmarksAsync(workflowState.Id, previousBookmarks, cancellationToken);
        await StoreBookmarksAsync(workflowState.Id, newBookmarks, cancellationToken);
        await PublishChangedBookmarksAsync(workflowState, previousBookmarks, newBookmarks, cancellationToken);
    }

    private async Task StoreBookmarksAsync(string workflowInstanceId, ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => (x.Name, x.Hash));

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var key = groupedBookmark.Key;
            var bookmarkIds = groupedBookmark.Select(x => x.Id).ToList();
            await _bookmarkStore.SaveAsync(key.Name, key.Hash, workflowInstanceId, bookmarkIds, cancellationToken);
        }
    }

    private async Task RemoveBookmarksAsync(string workflowInstanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => (x.Name, x.Hash));

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var key = groupedBookmark.Key;
            await _bookmarkStore.DeleteAsync(key.Name, key.Hash, workflowInstanceId, cancellationToken);
        }
    }

    private async Task PublishChangedBookmarksAsync(WorkflowState workflowState, ICollection<Bookmark> originalBookmarks, ICollection<Bookmark> updatedBookmarks, CancellationToken cancellationToken)
    {
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        var removedBookmarks = diff.Removed;
        var createdBookmarks = diff.Added;
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }
}