using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Handles the <see cref="CancelWorkflowsCommand"/>.
/// </summary>
[UsedImplicitly]
public class CancelWorkflowsCommandHandler(
    IWorkflowRuntime workflowRuntime,
    IWorkflowInstanceStore workflowInstanceStore,
    ISystemClock systemClock,
    ILogger<CancelWorkflowsCommandHandler> logger) : ICommandHandler<CancelWorkflowsCommand>
{
    /// <inheritdoc />
    public async Task<Unit> HandleAsync(CancelWorkflowsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var workflowInstanceId = command.Request.WorkflowInstanceId;
            var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken);
            await workflowClient.CancelAsync(cancellationToken);
            return Unit.Instance;
        }
        catch (Exception exception)
        {
            // BackgroundCommandProcessor logs this exception and does not retry the command.
            // That makes the failure terminal for the in-process dispatcher. Restore first, then rethrow
            // so the processor still records it. The compare-and-swap leaves a committed Cancelled row alone.
            await RestoreIfMarkedAsync(command, exception);
            throw;
        }
    }

    private async Task RestoreIfMarkedAsync(CancelWorkflowsCommand command, Exception failure)
    {
        var request = command.Request;
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
                    "Background cancellation of workflow instance {WorkflowInstanceId} failed, and the Cancelling mark was no longer current.",
                    request.WorkflowInstanceId);
            }
        }
        catch (Exception restoreFailure)
        {
            logger.LogError(
                restoreFailure,
                "Failed to restore workflow instance {WorkflowInstanceId} after background cancellation failed.",
                request.WorkflowInstanceId);
        }
    }
}
