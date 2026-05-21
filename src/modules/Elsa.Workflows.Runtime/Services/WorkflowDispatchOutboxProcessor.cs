using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
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
    ILogger<WorkflowDispatchOutboxProcessor> logger,
    ITenantAccessor? tenantAccessor = null) : IWorkflowDispatchOutboxProcessor
{
    private const string LockResource = "Elsa:WorkflowDispatchOutbox:Processor";

    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        IDistributedSynchronizationHandle? handle;

        try
        {
            handle = await distributedLockProvider.TryAcquireLockAsync(GetLockResource(), distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken);
        }
        catch (TimeoutException e) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(e, "Skipping workflow dispatch outbox processing because the processor lock could not be acquired.");
            return;
        }

        await using (handle)
        {
            if (handle == null)
            {
                logger.LogDebug("Skipping workflow dispatch outbox processing because another processor owns the lock.");
                return;
            }

            await ProcessPendingItemsAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryProcessAsync(CancellationToken cancellationToken = default)
    {
        IDistributedSynchronizationHandle? handle;

        try
        {
            handle = await distributedLockProvider.TryAcquireLockAsync(GetLockResource(), TimeSpan.Zero, cancellationToken);
        }
        catch (TimeoutException e) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(e, "Skipping eager workflow dispatch outbox processing because the processor lock could not be acquired.");
            return false;
        }

        await using (handle)
        {
            if (handle == null)
            {
                logger.LogDebug("Skipping eager workflow dispatch outbox processing because another processor owns the lock.");
                return false;
            }

            await ProcessPendingItemsAsync(cancellationToken);
            return true;
        }
    }

    private async Task ProcessPendingItemsAsync(CancellationToken cancellationToken)
    {
        var batchSize = Math.Max(1, dispatcherOptions.Value.OutboxProcessorBatchSize);
        var items = (await store.FindManyAsync(batchSize, cancellationToken)).ToList();

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
                logger.LogError(e, "Failed to process workflow dispatch outbox item {OutboxItemId}; processing will continue with the next item.", item.Id);
            }
        }
    }

    private string GetLockResource()
    {
        var tenantId = tenantAccessor?.Tenant?.Id;
        return string.IsNullOrWhiteSpace(tenantId) ? LockResource : $"{LockResource}:{tenantId}";
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
            await HandleUncommittedOwnerAsync(item, cancellationToken);
            return;
        }

        try
        {
            await SendAsync(item, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            await HandleDeliveryFailureAsync(item, e, cancellationToken);
            return;
        }

        await CleanupDeliveredItemAsync(item, cancellationToken);
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
            await TryDeleteRetainedItemAsync(item, cancellationToken);
            return;
        }

        logger.LogDebug("Skipping workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} was not found; it will be retained until {ExpiresAt}", item.Id, item.OwnerWorkflowInstanceId, expiresAt);
    }

    private async Task HandleUncommittedOwnerAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var retention = dispatcherOptions.Value.OrphanedOutboxItemRetention;
        var expiresAt = item.CreatedAt.Add(retention);

        if (retention <= TimeSpan.Zero || systemClock.UtcNow >= expiresAt)
        {
            logger.LogWarning("Deleting workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} never committed the outbox marker and the retention period has elapsed", item.Id, item.OwnerWorkflowInstanceId);
            await TryDeleteRetainedItemAsync(item, cancellationToken);
            return;
        }

        logger.LogDebug("Skipping workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} has not committed it; it will be retained until {ExpiresAt}", item.Id, item.OwnerWorkflowInstanceId, expiresAt);
    }

    private async Task TryDeleteRetainedItemAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        try
        {
            await store.DeleteAsync(item.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to delete retained workflow dispatch outbox item {OutboxItemId}; it will be retried without counting as a delivery failure.", item.Id);
        }
    }

    private async Task HandleDeliveryFailureAsync(WorkflowDispatchOutboxItem item, Exception exception, CancellationToken cancellationToken)
    {
        item.DeliveryAttempts++;

        try
        {
            if (item.DeliveryAttempts >= dispatcherOptions.Value.MaxOutboxDeliveryAttempts)
            {
                logger.LogWarning(exception, "Abandoning workflow dispatch outbox item {OutboxItemId} after {DeliveryAttempts} failed delivery attempts", item.Id, item.DeliveryAttempts);
                if (await TryDeleteAbandonedItemAsync(item, cancellationToken))
                    await RemoveCommittedMarkerAsync(item, cancellationToken);
                return;
            }

            await store.SaveAsync(item, cancellationToken);
            logger.LogError(exception, "Failed to deliver workflow dispatch outbox item {OutboxItemId}; attempt {DeliveryAttempts} of {MaxDeliveryAttempts}", item.Id, item.DeliveryAttempts, dispatcherOptions.Value.MaxOutboxDeliveryAttempts);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to persist delivery failure state for workflow dispatch outbox item {OutboxItemId}; processing will continue with the next item.", item.Id);
        }
    }

    private async Task<bool> TryDeleteAbandonedItemAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        try
        {
            await store.DeleteAsync(item.Id, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to delete abandoned workflow dispatch outbox item {OutboxItemId}; preserving its delivery attempt count for the next processor run.", item.Id);
        }

        try
        {
            await store.SaveAsync(item, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to preserve delivery attempt count for abandoned workflow dispatch outbox item {OutboxItemId}.", item.Id);
        }

        return false;
    }

    private async Task CleanupDeliveredItemAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        try
        {
            await store.DeleteAsync(item.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Delivered workflow dispatch outbox item {OutboxItemId}, but failed to delete it from the outbox store. The item was not counted as a delivery failure.", item.Id);
            return;
        }

        await RemoveCommittedMarkerAsync(item, cancellationToken);
    }

    private async Task RemoveCommittedMarkerAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var lockResource = $"workflow-instance:{item.OwnerWorkflowInstanceId}";

        try
        {
            await using var handle = await distributedLockProvider.AcquireLockAsync(lockResource, distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken);
            var owner = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = item.OwnerWorkflowInstanceId }, cancellationToken);

            if (owner == null)
                return;

            if (!owner.WorkflowState.RemoveWorkflowDispatchOutboxItem(item.Id))
                return;

            await workflowInstanceStore.SaveAsync(owner, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to remove delivered workflow dispatch outbox item {OutboxItemId} from owner workflow {WorkflowInstanceId}; future commits may prune the marker during a later sweep.", item.Id, item.OwnerWorkflowInstanceId);
        }
    }
}
