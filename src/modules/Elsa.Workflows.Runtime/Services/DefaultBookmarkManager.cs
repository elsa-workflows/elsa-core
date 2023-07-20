using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IBookmarkManager"/>.
/// </summary>
public class DefaultBookmarkManager : IBookmarkManager
{
    private readonly IBookmarkStore _bookmarkStore;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBookmarkManager"/> class.
    /// </summary>
    public DefaultBookmarkManager(IBookmarkStore bookmarkStore, INotificationSender notificationSender)
    {
        _bookmarkStore = bookmarkStore;
        _notificationSender = notificationSender;
    }
    
    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
    {
        var bookmarks = (await _bookmarkStore.FindManyAsync(filter, cancellationToken)).ToList();
        await _notificationSender.SendAsync(new BookmarksDeleting(bookmarks), cancellationToken);
        var count = await _bookmarkStore.DeleteAsync(filter, cancellationToken);
        await _notificationSender.SendAsync(new BookmarksDeleted(bookmarks), cancellationToken);
        return count;
    }
}