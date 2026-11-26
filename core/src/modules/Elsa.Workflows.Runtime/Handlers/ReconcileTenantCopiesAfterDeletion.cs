using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// When a tenant deletes the last published version of its own workflow definition, it stops overriding a tenant-agnostic definition with
/// the same ID. Reconciles the tenant's scheduled triggers of that tenant-agnostic definition again.
/// </summary>
[UsedImplicitly]
internal class ReconcileTenantCopiesAfterDeletion(ITriggerIndexer triggerIndexer) :
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>
{
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => ReconcileAsync([notification.DefinitionId], cancellationToken);
    public Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => ReconcileAsync(notification.DefinitionIds, cancellationToken);
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) => ReconcileAsync([notification.WorkflowDefinition.DefinitionId], cancellationToken);
    public Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => ReconcileAsync(notification.DefinitionIds, cancellationToken);

    private Task ReconcileAsync(ICollection<string> definitionIds, CancellationToken cancellationToken) =>
        triggerIndexer is TriggerIndexer indexer && definitionIds.Count > 0
            ? indexer.ReconcileTenantCopiesAfterDeletionAsync(definitionIds, cancellationToken)
            : Task.CompletedTask;
}
