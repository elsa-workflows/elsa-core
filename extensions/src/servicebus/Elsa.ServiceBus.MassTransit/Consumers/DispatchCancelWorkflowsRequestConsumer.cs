using Elsa.Common;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.ServiceBus.MassTransit.Consumers;

/// <summary>
/// Consumes a <see cref="DispatchCancelWorkflowRequest"/> to cancel workflows.
/// </summary>
[UsedImplicitly]
public class DispatchCancelWorkflowsRequestConsumer(
    IWorkflowRuntime workflowRuntime,
    IWorkflowInstanceStore workflowInstanceStore,
    ISystemClock systemClock,
    ILogger<DispatchCancelWorkflowsRequestConsumer> logger) : IConsumer<DispatchCancelWorkflowRequest>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchCancelWorkflowRequest> context)
    {
        var request = context.Message;

        try
        {
            var cancellationToken = context.CancellationToken;
            var client = await workflowRuntime.CreateClientAsync(request.WorkflowInstanceId, cancellationToken);
            await client.CancelAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // This module does not configure a retry policy for the consumer. A host policy decides whether to
            // retry only after this method throws, so the consumer cannot tell a terminal attempt from the first
            // of several. Roll the interim mark back, then rethrow so that policy still runs. A retry cancels
            // from the restored sub-status. The compare-and-swap does nothing if Cancelled was already committed.
            await RestoreIfMarkedAsync(request, exception);
            throw;
        }
    }

    private async Task RestoreIfMarkedAsync(DispatchCancelWorkflowRequest request, Exception failure)
    {
        if (request.PreviousSubStatus is not { } previousSubStatus || request.CancellingAt is not { } cancellingAt)
            return;

        try
        {
            var restored = await workflowInstanceStore.TryRestoreAsync(
                request.WorkflowInstanceId,
                previousSubStatus,
                cancellingAt,
                systemClock.UtcNow.TruncateToMilliseconds(),
                CancellationToken.None);

            if (!restored)
            {
                logger.LogDebug(
                    failure,
                    "Cancellation of workflow instance {WorkflowInstanceId} failed, and the Cancelling mark was no longer current.",
                    request.WorkflowInstanceId);
            }
        }
        catch (Exception restoreFailure)
        {
            logger.LogError(
                restoreFailure,
                "Failed to restore workflow instance {WorkflowInstanceId} after cancellation failed.",
                request.WorkflowInstanceId);
        }
    }
}
