using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes bookmarks for workflow instances being deleted.
/// </summary>
[UsedImplicitly]
public class DeleteBookmarks(IBookmarkManager bookmarkManager) : INotificationHandler<WorkflowInstancesDeleting>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowInstancesDeleting notification, CancellationToken cancellationToken)
    {
        await bookmarkManager.DeleteManyAsync(new BookmarkFilter { WorkflowInstanceIds = notification.Ids }, cancellationToken);
    }
}