using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Notifications;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes workflow execution log records in response to the <see cref="WorkflowInstancesDeleting"/> notification.
/// </summary>
internal class WorkflowExecutionContextNotificationsHandler :
    INotificationHandler<WorkflowExecuting>,
    INotificationHandler<WorkflowExecuted> 
{
    private readonly IWorkflowExecutionContextStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteWorkflowExecutionLogRecords"/> class.
    /// </summary>
    public WorkflowExecutionContextNotificationsHandler(IWorkflowExecutionContextStore store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public Task HandleAsync(WorkflowExecuting notification, CancellationToken cancellationToken)
    {
        return _store.SaveAsync(notification.WorkflowExecutionContext);
    }
    
    /// <inheritdoc />
    public Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        return _store.DeleteAsync(notification.WorkflowExecutionContext.Id);
    }
}