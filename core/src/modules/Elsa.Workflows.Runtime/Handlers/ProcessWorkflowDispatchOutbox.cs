using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Processes the workflow dispatch outbox after a workflow state commit.
/// </summary>
public class ProcessWorkflowDispatchOutbox(IServiceProvider serviceProvider, IOptions<WorkflowDispatcherOptions> options, ILogger<ProcessWorkflowDispatchOutbox> logger) : INotificationHandler<WorkflowStateCommitted>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
    {
        if (!options.Value.UseTransactionalOutbox || !options.Value.ProcessOutboxAfterCommit)
            return;

        if (!notification.WorkflowState.HasWorkflowDispatchOutboxItems())
            return;

        try
        {
            var processor = serviceProvider.GetRequiredService<IWorkflowDispatchOutboxProcessor>();
            await processor.TryProcessAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to process workflow dispatch outbox after workflow state commit; the recurring outbox sweep will retry pending items.");
        }
    }
}
