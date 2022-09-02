using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class BookmarkManager : IBookmarkManager
{
    private readonly IWorkflowBookmarkStore _bookmarkStore;
    private readonly IEventPublisher _eventPublisher;

    public BookmarkManager(IWorkflowBookmarkStore bookmarkStore, IEventPublisher eventPublisher)
    {
        _bookmarkStore = bookmarkStore;
        _eventPublisher = eventPublisher;
    }

    public async Task DeleteAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var list = bookmarks.ToList();
        var ids = list.Select(x => x.Id).ToList();
        await _bookmarkStore.DeleteManyAsync(ids, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksDeleted(list), cancellationToken);
    }

    public async Task SaveAsync(WorkflowInstance workflowInstance, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var list = bookmarks.ToList();
        var workflowBookmarks = list.Select(x => WorkflowBookmark.FromBookmark(x, workflowInstance)).ToList();
        await _bookmarkStore.SaveManyAsync(workflowBookmarks, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksSaved(list), cancellationToken);
    }

    public async Task<Bookmark?> FindByIdAsync(string id, CancellationToken cancellationToken)
    {
        var workflowBookmark = await _bookmarkStore.FindByIdAsync(id, cancellationToken);
        return workflowBookmark?.ToBookmark();
    }

    public async Task<IEnumerable<Bookmark>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var workflowBookmarks = await _bookmarkStore.FindManyByWorkflowInstanceIdAsync(workflowInstanceId, cancellationToken);
        return workflowBookmarks.Select(x => x.ToBookmark());
    }

    public async Task<IEnumerable<WorkflowBookmark>> FindManyByHashAsync(string bookmarkName, string hash, CancellationToken cancellationToken = default)
    {
        return await _bookmarkStore.FindManyAsync(bookmarkName, hash, cancellationToken);
    }
}