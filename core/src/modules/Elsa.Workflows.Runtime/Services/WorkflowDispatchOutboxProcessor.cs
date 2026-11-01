using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
/// <remarks>
/// Items are delivered through the inner <see cref="IWorkflowDispatcher"/> on their persisted channel. An unroutable
/// channel (not configured in <see cref="WorkflowDispatcherOptions.Channels"/>, or answered with
/// <see cref="RuntimeFaultCodes.UnknownChannel"/> by the inner dispatcher) fails closed: the item is never delivered on the
/// default queue. Each such pass counts as a failed delivery attempt and logs an error naming the channel, so the item is
/// abandoned at <see cref="WorkflowDispatcherOptions.MaxOutboxDeliveryAttempts"/> with a warning naming the channel instead
/// of filling the batch and stalling newer dispatches (#8816). When the inner dispatcher cannot route channels (for example
/// <see cref="BackgroundWorkflowDispatcher"/>), a configured channel is ignored and the item goes to the default queue,
/// the same as before #8167. Follow-up: #8813.
/// </remarks>
public class WorkflowDispatchOutboxProcessor(
    IWorkflowDispatchOutboxMarkerCleaner markerCleaner,
    IWorkflowDispatchOutboxStore store,
    IWorkflowInstanceStore workflowInstanceStore,
    [FromKeyedServices(WorkflowDispatcherServiceKeys.Inner)] IWorkflowDispatcher innerDispatcher,
    IDistributedLockProvider distributedLockProvider,
    ISystemClock systemClock,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    IOptions<WorkflowDispatcherOptions> dispatcherOptions,
    ILogger<WorkflowDispatchOutboxProcessor> logger,
    ITenantAccessor? tenantAccessor = null,
    ITenantFinder? tenantFinder = null) : IWorkflowDispatchOutboxProcessor
{
    private const string LockResource = "Elsa:WorkflowDispatchOutbox:Processor";

    /// <summary>
    /// Creates a processor with the default distributed-lock-based cleanup of committed outbox markers.
    /// </summary>
    public WorkflowDispatchOutboxProcessor(
        IWorkflowDispatchOutboxStore store,
        IWorkflowInstanceStore workflowInstanceStore,
        [FromKeyedServices(WorkflowDispatcherServiceKeys.Inner)] IWorkflowDispatcher innerDispatcher,
        IDistributedLockProvider distributedLockProvider,
        ISystemClock systemClock,
        IOptions<DistributedLockingOptions> distributedLockingOptions,
        IOptions<WorkflowDispatcherOptions> dispatcherOptions,
        ILogger<WorkflowDispatchOutboxProcessor> logger,
        ITenantAccessor? tenantAccessor = null,
        ITenantFinder? tenantFinder = null)
        : this(new WorkflowDispatchOutboxMarkerCleaner(workflowInstanceStore, distributedLockProvider, distributedLockingOptions),
            store, workflowInstanceStore, innerDispatcher, distributedLockProvider, systemClock,
            distributedLockingOptions, dispatcherOptions, logger, tenantAccessor, tenantFinder)
    {
    }

    /// <summary>
    /// Creates a processor that delivers through mediator background commands, as this processor did before it delivered
    /// through the inner <see cref="IWorkflowDispatcher"/>. This overload cannot route channels, so items with a channel fail
    /// closed and are abandoned at the attempt limit. It deliberately does not use the registered <see cref="IWorkflowDispatcher"/>: with the outbox enabled that is
    /// the transactional dispatcher, which would enqueue each item again instead of delivering it.
    /// </summary>
    [Obsolete("This constructor uses the old non-durable delivery: the outbox item is deleted as soon as it is handed to the in-memory background queue, so a node stop before that queue runs can lose the child workflow. Use the constructor that takes the inner IWorkflowDispatcher (keyed WorkflowDispatcherServiceKeys.Inner). This overload will be removed in a future version.")]
    public WorkflowDispatchOutboxProcessor(
        IWorkflowDispatchOutboxStore store,
        IWorkflowInstanceStore workflowInstanceStore,
        ICommandSender commandSender,
        IDistributedLockProvider distributedLockProvider,
        ISystemClock systemClock,
        IOptions<DistributedLockingOptions> distributedLockingOptions,
        IOptions<WorkflowDispatcherOptions> dispatcherOptions,
        ILogger<WorkflowDispatchOutboxProcessor> logger,
        ITenantAccessor? tenantAccessor = null)
        : this(store, workflowInstanceStore, new CommandSenderWorkflowDispatcher(commandSender, tenantAccessor), distributedLockProvider, systemClock,
            distributedLockingOptions, dispatcherOptions, logger, tenantAccessor)
    {
    }

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
            logger.LogDebug(e, "Skipping workflow dispatch outbox processing because the processor lock could not be acquired.");
            return false;
        }

        await using (handle)
        {
            if (handle == null)
            {
                logger.LogDebug("Skipping workflow dispatch outbox processing because another processor owns the lock.");
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
        // Fail closed: an unroutable channel must never silently fall back to the default channel. The item stays in the
        // outbox and counts a delivery attempt, so it is abandoned at the attempt limit.
        if (!string.IsNullOrEmpty(item.Channel) && dispatcherOptions.Value.Channels.All(x => x.Name != item.Channel))
        {
            throw new InvalidOperationException($"Cannot deliver workflow dispatch outbox item {item.Id}: dispatch channel '{item.Channel}' is not configured.");
        }

        // Push the tenant here rather than in an async helper: the tenant context is async-local and would not flow back.
        var tenant = await FindItemTenantAsync(item, cancellationToken);
        using var tenantContext = tenant != null ? tenantAccessor!.PushContext(tenant) : null;
        var options = new DispatchWorkflowOptions { Channel = item.Channel };

        var response = item.Kind switch
        {
            WorkflowDispatchOutboxItemKind.WorkflowDefinition when item.WorkflowDefinitionCommand is { } command => await DispatchDefinitionAsync(command, options, cancellationToken),
            WorkflowDispatchOutboxItemKind.WorkflowInstance when item.WorkflowInstanceCommand is { } command => await innerDispatcher.DispatchAsync(new DispatchWorkflowInstanceRequest(command.InstanceId)
            {
                BookmarkId = command.BookmarkId,
                ActivityHandle = command.ActivityHandle,
                Input = command.Input,
                Properties = command.Properties,
                CorrelationId = command.CorrelationId
            }, options, cancellationToken),
            WorkflowDispatchOutboxItemKind.TriggerWorkflows when item.TriggerWorkflowsCommand is { } command => await innerDispatcher.DispatchAsync(new DispatchTriggerWorkflowsRequest(command.ActivityTypeName, command.Stimulus)
            {
                CorrelationId = command.CorrelationId,
                WorkflowInstanceId = command.WorkflowInstanceId,
                ActivityInstanceId = command.ActivityInstanceId,
                Input = command.Input,
                Properties = command.Properties
            }, options, cancellationToken),
            WorkflowDispatchOutboxItemKind.ResumeWorkflows when item.ResumeWorkflowsCommand is { } command => await innerDispatcher.DispatchAsync(new DispatchResumeWorkflowsRequest(command.ActivityTypeName, command.Stimulus)
            {
                CorrelationId = command.CorrelationId,
                WorkflowInstanceId = command.WorkflowInstanceId,
                ActivityInstanceId = command.ActivityInstanceId,
                Input = command.Input
            }, options, cancellationToken),
            _ => throw new InvalidOperationException($"Outbox item {item.Id} does not contain a dispatch command for kind {item.Kind}.")
        };

        if (response.Fault?.Code == RuntimeFaultCodes.UnknownChannel)
        {
            throw new InvalidOperationException($"Delivery of workflow dispatch outbox item {item.Id} was rejected: dispatch channel '{item.Channel ?? "(default)"}' is unknown to the inner dispatcher.", response.Fault);
        }

        if (!response.Succeeded)
        {
            throw new InvalidOperationException($"Delivery of workflow dispatch outbox item {item.Id} on channel '{item.Channel ?? "(default)"}' was rejected: {response.Fault!.Message}", response.Fault);
        }
        logger.LogDebug("Delivered workflow dispatch outbox item {OutboxItemId} for owner workflow {WorkflowInstanceId} on channel {Channel}", item.Id, item.OwnerWorkflowInstanceId, item.Channel ?? "(default)");
    }

    private Task<DispatchWorkflowResponse> DispatchDefinitionAsync(DispatchWorkflowDefinitionCommand command, DispatchWorkflowOptions options, CancellationToken cancellationToken)
    {
        return innerDispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(command.DefinitionVersionId)
        {
            ParentWorkflowInstanceId = command.ParentWorkflowInstanceId,
            Input = command.Input,
            Properties = command.Properties,
            CorrelationId = command.CorrelationId,
            InstanceId = command.InstanceId,
            TriggerActivityId = command.TriggerActivityId,
            SchedulingActivityExecutionId = command.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = command.SchedulingWorkflowInstanceId,
            SchedulingCallStackDepth = command.SchedulingCallStackDepth,
            // The child's instance ID is fixed at enqueue; the dispatcher checks for it right before creating the child,
            // so a retried delivery cannot create it twice.
            SkipIfInstanceExists = command.SkipIfInstanceExists
        }, options, cancellationToken);
    }

    /// <summary>Finds the tenant the item was enqueued for, or null when the current tenant context already matches.</summary>
    private async Task<Tenant?> FindItemTenantAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        if (tenantAccessor == null || string.IsNullOrEmpty(item.TenantId) || tenantAccessor.Tenant?.Id == item.TenantId)
        {
            return null;
        }

        var tenant = tenantFinder != null ? await tenantFinder.FindByIdAsync(item.TenantId, cancellationToken) : null;
        return tenant ?? throw new InvalidOperationException($"Cannot deliver workflow dispatch outbox item {item.Id}: tenant '{item.TenantId}' was not found.");
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
                logger.LogWarning(exception, "Abandoning workflow dispatch outbox item {OutboxItemId} on channel '{Channel}' after {DeliveryAttempts} failed delivery attempts", item.Id, item.Channel ?? "(default)", item.DeliveryAttempts);
                if (await TryDeleteAbandonedItemAsync(item, cancellationToken))
                {
                    await RemoveCommittedMarkerAsync(item, cancellationToken);
                }
                return;
            }

            await store.SaveAsync(item, cancellationToken);
            logger.LogError(exception, "Failed to deliver workflow dispatch outbox item {OutboxItemId} on channel '{Channel}'; attempt {DeliveryAttempts} of {MaxDeliveryAttempts}", item.Id, item.Channel ?? "(default)", item.DeliveryAttempts, dispatcherOptions.Value.MaxOutboxDeliveryAttempts);
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
        try
        {
            await markerCleaner.RemoveAsync(item, cancellationToken);
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

    /// <summary>Delivers through mediator background commands, for the obsolete constructor only. It cannot route channels.</summary>
    private sealed class CommandSenderWorkflowDispatcher(ICommandSender commandSender, ITenantAccessor? tenantAccessor) : IWorkflowDispatcher
    {
        public Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
            SendAsync(WorkflowDispatchCommandFactory.CreateCommand(request), options, cancellationToken);

        public Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
            SendAsync(WorkflowDispatchCommandFactory.CreateCommand(request), options, cancellationToken);

        public Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
            SendAsync(WorkflowDispatchCommandFactory.CreateCommand(request), options, cancellationToken);

        public Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
            SendAsync(WorkflowDispatchCommandFactory.CreateCommand(request), options, cancellationToken);

        private async Task<DispatchWorkflowResponse> SendAsync<TCommand>(TCommand command, DispatchWorkflowOptions? options, CancellationToken cancellationToken) where TCommand : ICommand<Unit>
        {
            // Fail closed: background commands have no channels, so never deliver a channel-bound item on the default queue.
            if (!string.IsNullOrEmpty(options?.Channel))
            {
                return DispatchWorkflowResponse.UnknownChannel();
            }

            await commandSender.SendAsync(command, CommandStrategy.Background, TenantHeaders.CreateHeaders(tenantAccessor?.TenantId), cancellationToken);
            return DispatchWorkflowResponse.Success();
        }
    }
}