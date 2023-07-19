using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes bookmarks for workflow instances being deleted.
/// </summary>
public class DeleteBookmarks : INotificationHandler<WorkflowInstancesDeleting>
{
    private readonly IBookmarkStore _bookmarkStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteBookmarks"/> class.
    /// </summary>
    public DeleteBookmarks(IBookmarkStore bookmarkStore)
    {
        _bookmarkStore = bookmarkStore;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowInstancesDeleting notification, CancellationToken cancellationToken)
    {
        await _bookmarkStore.DeleteAsync(new BookmarkFilter { WorkflowInstanceIds = notification.Ids }, cancellationToken);
    }
}