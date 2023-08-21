using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Handles <see cref="WorkflowInboxMessageReceived"/> notifications and tries to forward the message to the appropriate workflow instance.
/// If no workflow was found that was ready to handle the message, the message remains in the inbox.
/// </summary>
public class ReadWorkflowInboxMessage : INotificationHandler<WorkflowInboxMessageReceived>
{
    private readonly IWorkflowInbox _workflowInbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWorkflowInboxMessage"/> class.
    /// </summary>
    public ReadWorkflowInboxMessage(IWorkflowInbox workflowInbox)
    {
        _workflowInbox = workflowInbox;
    }
    
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowInboxMessageReceived notification, CancellationToken cancellationToken)
    {
        var message = notification.InboxMessage;
        var result = await _workflowInbox.BroadcastAsync(message, cancellationToken);
        notification.WorkflowExecutionResults.AddRange(result.WorkflowExecutionResults);
    }
}