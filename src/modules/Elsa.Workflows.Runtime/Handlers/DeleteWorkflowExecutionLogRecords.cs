using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes workflow execution log records in response to the <see cref="WorkflowInstancesDeleting"/> notification.
/// </summary>
[PublicAPI]
internal class DeleteWorkflowExecutionLogRecords : INotificationHandler<WorkflowInstancesDeleting>
{
    private readonly IWorkflowExecutionLogStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteWorkflowExecutionLogRecords"/> class.
    /// </summary>
    public DeleteWorkflowExecutionLogRecords(IWorkflowExecutionLogStore store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowInstancesDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteManyAsync(new WorkflowExecutionLogRecordFilter { WorkflowInstanceIds = notification.Ids }, cancellationToken);
    }
    
    private async Task DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        await _store.DeleteManyAsync(filter, cancellationToken);
    }
}