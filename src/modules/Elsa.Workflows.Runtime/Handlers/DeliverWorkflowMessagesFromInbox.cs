using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Checks the inbox for messages and resumes workflows that are waiting for a message based on the bookmarks that were just created.
/// </summary>
public class DeliverWorkflowMessagesFromInbox : INotificationHandler<WorkflowBookmarksPersisted>
{
    private readonly IWorkflowInbox _workflowInbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliverWorkflowMessagesFromInbox"/> class.
    /// </summary>
    public DeliverWorkflowMessagesFromInbox(IWorkflowInbox workflowInbox)
    {
        _workflowInbox = workflowInbox;
    }
    
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowBookmarksPersisted notification, CancellationToken cancellationToken)
    {
        var addedBookmarks = notification.Diff.Added;

        foreach (var bookmark in addedBookmarks)
        {
            var activityTypeName = bookmark.Name;
            var hash = bookmark.Hash;
            
            var filter = new WorkflowInboxMessageFilter
            {
                ActivityTypeName = activityTypeName,
                Hash = hash
            };
            
            var messages = await _workflowInbox.FindManyAsync(filter, cancellationToken);
            
            foreach (var message in messages) 
                await _workflowInbox.DeliverAsync(message, cancellationToken);
        }
    }
}