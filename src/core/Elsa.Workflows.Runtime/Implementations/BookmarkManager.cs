using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;
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

    public async Task DeleteBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var list = bookmarks.ToList();
        var ids = list.Select(x => x.Id).ToList();
        await _bookmarkStore.DeleteManyAsync(ids, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksDeleted(list), cancellationToken);
    }

    public async Task SaveBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var list = bookmarks.ToList();
        await _bookmarkStore.SaveManyAsync(list, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksSaved(list), cancellationToken);
    }
}