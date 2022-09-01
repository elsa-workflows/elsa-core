using Elsa.Mediator.Services;
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

    public async Task DeleteBookmarksAsync(IEnumerable<WorkflowBookmark> workflowBookmarks, CancellationToken cancellationToken = default)
    {
        var list = workflowBookmarks.ToList();
        var ids = list.Select(x => x.Id).ToList();
        var bookmarks = list.Select(x => x.ToBookmark()).ToList();
        await _bookmarkStore.DeleteManyAsync(ids, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksDeleted(bookmarks), cancellationToken);
    }

    public async Task SaveBookmarksAsync(IEnumerable<WorkflowBookmark> workflowBookmarks, CancellationToken cancellationToken = default)
    {
        var list = workflowBookmarks.ToList();
        var bookmarks = list.Select(x => x.ToBookmark()).ToList();
        await _bookmarkStore.SaveManyAsync(list, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksSaved(bookmarks), cancellationToken);
    }
}