using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers.Notifications;

/// <summary>
/// A workflow definition notifications handler for evicting the cache of the workflow definition service.
/// </summary>
/// <remarks>
/// This service listens for specific notifications and triggers cache invalidation operations accordingly.
/// </remarks>
[UsedImplicitly]
internal class EvictWorkflowDefinitionServiceCache(IWorkflowDefinitionCacheManager workflowDefinitionCacheManager) :
    INotificationHandler<WorkflowDefinitionDraftSaving>,
    INotificationHandler<WorkflowDefinitionPublishing>,
    INotificationHandler<WorkflowDefinitionRetracting>,
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsUpdating>,
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>
{
    public Task HandleAsync(WorkflowDefinitionDraftSaving notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }
    
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionPublishing notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracting notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken)
    {
        foreach (var definitionId in notification.DefinitionIds)
            await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(definitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsUpdating notification, CancellationToken cancellationToken)
    {
        foreach (var definition in notification.WorkflowDefinitions)
            await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(definition.DefinitionId, cancellationToken);
    }

    // Evict again once the store writes are done, so a read between the "-ing" eviction and the save cannot leave a stale entry behind.

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        return workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (var definitionId in notification.DefinitionIds)
            await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(definitionId, cancellationToken);
    }
}
