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
    private readonly IBookmarkManager _bookmarkManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteBookmarks"/> class.
    /// </summary>
    public DeleteBookmarks(IBookmarkManager bookmarkManager)
    {
        _bookmarkManager = bookmarkManager;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowInstancesDeleting notification, CancellationToken cancellationToken)
    {
        await _bookmarkManager.DeleteManyAsync(new BookmarkFilter { WorkflowInstanceIds = notification.Ids }, cancellationToken);
    }
}