using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Notifications;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Implementations;

public class BookmarkManager : IBookmarkManager
{
    private readonly ICommandSender _commandSender;
    private readonly IEventPublisher _eventPublisher;

    public BookmarkManager(ICommandSender commandSender, IEventPublisher eventPublisher)
    {
        _commandSender = commandSender;
        _eventPublisher = eventPublisher;
    }

    public async Task DeleteBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var list = bookmarks.ToList();
        var ids = list.Select(x => x.Id).ToList();
        await _commandSender.ExecuteAsync(new DeleteWorkflowBookmarks(ids), cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksDeleted(list), cancellationToken);
    }

    public async Task SaveBookmarksAsync(IEnumerable<WorkflowBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var list = bookmarks.ToList();
        await _commandSender.ExecuteAsync(new SaveWorkflowBookmarks(list), cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowBookmarksSaved(list), cancellationToken);
    }
}