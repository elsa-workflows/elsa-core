using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Deletes the triggers of workflow definitions being deleted. After a tenant deleted versions of its own definition, it may have stopped
/// overriding a tenant-agnostic definition with the same ID, so the tenant's scheduled triggers of that definition are reconciled again.
/// </summary>
[UsedImplicitly]
public class DeleteTriggers(ITriggerIndexer triggerIndexer) :
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionVersionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsDeleting>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionId = notification.DefinitionId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionVersionId = notification.WorkflowDefinition.Id
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionIds = notification.DefinitionIds
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleting notification, CancellationToken cancellationToken)
    {
        await DeleteTriggersAsync(new TriggerFilter
        {
            WorkflowDefinitionVersionIds = notification.Ids,
            RequireCompleteCleanup = notification.RequireCompleteTriggerCleanup
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync([notification.DefinitionId], cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync(notification.DefinitionIds, cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync([notification.WorkflowDefinition.DefinitionId], cancellationToken);

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => ReconcileTenantCopiesAsync(notification.DefinitionIds, cancellationToken);

    private Task ReconcileTenantCopiesAsync(ICollection<string> definitionIds, CancellationToken cancellationToken) =>
        triggerIndexer is TriggerIndexer indexer && definitionIds.Count > 0
            ? indexer.ReconcileTenantCopiesAfterDeletionAsync(definitionIds, cancellationToken)
            : Task.CompletedTask;

    private async Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        await triggerIndexer.DeleteTriggersAsync(filter, cancellationToken);
    }
}