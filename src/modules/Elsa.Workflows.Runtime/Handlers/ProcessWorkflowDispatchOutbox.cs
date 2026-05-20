using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Processes the workflow dispatch outbox after a workflow state commit.
/// </summary>
public class ProcessWorkflowDispatchOutbox(IWorkflowDispatchOutboxProcessor processor, IOptions<WorkflowDispatcherOptions> options) : INotificationHandler<WorkflowStateCommitted>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
    {
        if (!options.Value.UseTransactionalOutbox || !options.Value.ProcessOutboxAfterCommit)
            return;

        if (!notification.WorkflowState.HasWorkflowDispatchOutboxItems())
            return;

        await processor.TryProcessAsync(cancellationToken);
    }
}
