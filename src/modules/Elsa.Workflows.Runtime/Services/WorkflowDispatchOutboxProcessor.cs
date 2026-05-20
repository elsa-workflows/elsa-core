using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.DistributedHosting;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDispatchOutboxProcessor(
    IWorkflowDispatchOutboxStore store,
    IWorkflowInstanceStore workflowInstanceStore,
    ICommandSender commandSender,
    IDistributedLockProvider distributedLockProvider,
    ISystemClock systemClock,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    IOptions<WorkflowDispatcherOptions> dispatcherOptions,
    ILogger<WorkflowDispatchOutboxProcessor> logger) : IWorkflowDispatchOutboxProcessor
{
    private const string LockResource = "Elsa:WorkflowDispatchOutbox:Processor";

    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await using (await distributedLockProvider.AcquireLockAsync(LockResource, distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken))
        {
            var items = (await store.FindManyAsync(cancellationToken)).OrderBy(x => x.CreatedAt).ToList();

            foreach (var item in items)
            {
                try
                {
                    await ProcessAsync(item, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await HandleDeliveryFailureAsync(item, e, cancellationToken);
                }
            }
        }
    }

    private async Task ProcessAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var owner = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = item.OwnerWorkflowInstanceId }, cancellationToken);

        if (owner == null)
        {
            await HandleMissingOwnerAsync(item, cancellationToken);
            return;
        }

        if (!owner.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id))
        {
            logger.LogDebug("Skipping workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} has not committed it", item.Id, item.OwnerWorkflowInstanceId);
            return;
        }

        await SendAsync(item, cancellationToken);
        await store.DeleteAsync(item.Id, cancellationToken);
    }

    private async Task SendAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var headers = TenantHeaders.CreateHeaders(item.TenantId);

        switch (item.Kind)
        {
            case WorkflowDispatchOutboxItemKind.WorkflowDefinition when item.WorkflowDefinitionCommand != null:
                await commandSender.SendAsync(item.WorkflowDefinitionCommand, CommandStrategy.Background, headers, cancellationToken);
                break;
            case WorkflowDispatchOutboxItemKind.WorkflowInstance when item.WorkflowInstanceCommand != null:
                await commandSender.SendAsync(item.WorkflowInstanceCommand, CommandStrategy.Background, headers, cancellationToken);
                break;
            case WorkflowDispatchOutboxItemKind.TriggerWorkflows when item.TriggerWorkflowsCommand != null:
                await commandSender.SendAsync(item.TriggerWorkflowsCommand, CommandStrategy.Background, headers, cancellationToken);
                break;
            case WorkflowDispatchOutboxItemKind.ResumeWorkflows when item.ResumeWorkflowsCommand != null:
                await commandSender.SendAsync(item.ResumeWorkflowsCommand, CommandStrategy.Background, headers, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Outbox item {item.Id} does not contain a dispatch command for kind {item.Kind}.");
        }

        logger.LogDebug("Delivered workflow dispatch outbox item {OutboxItemId} for owner workflow {WorkflowInstanceId}", item.Id, item.OwnerWorkflowInstanceId);
    }

    private async Task HandleMissingOwnerAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var retention = dispatcherOptions.Value.OrphanedOutboxItemRetention;
        var expiresAt = item.CreatedAt.Add(retention);

        if (retention <= TimeSpan.Zero || systemClock.UtcNow >= expiresAt)
        {
            logger.LogWarning("Deleting workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} was not found and the orphan retention period has elapsed", item.Id, item.OwnerWorkflowInstanceId);
            await store.DeleteAsync(item.Id, cancellationToken);
            return;
        }

        logger.LogDebug("Skipping workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} was not found; it will be retained until {ExpiresAt}", item.Id, item.OwnerWorkflowInstanceId, expiresAt);
    }

    private async Task HandleDeliveryFailureAsync(WorkflowDispatchOutboxItem item, Exception exception, CancellationToken cancellationToken)
    {
        item.DeliveryAttempts++;

        if (item.DeliveryAttempts >= dispatcherOptions.Value.MaxOutboxDeliveryAttempts)
        {
            logger.LogWarning(exception, "Abandoning workflow dispatch outbox item {OutboxItemId} after {DeliveryAttempts} failed delivery attempts", item.Id, item.DeliveryAttempts);
            await store.DeleteAsync(item.Id, cancellationToken);
            return;
        }

        await store.SaveAsync(item, cancellationToken);
        logger.LogError(exception, "Failed to deliver workflow dispatch outbox item {OutboxItemId}; attempt {DeliveryAttempts} of {MaxDeliveryAttempts}", item.Id, item.DeliveryAttempts, dispatcherOptions.Value.MaxOutboxDeliveryAttempts);
    }
}
