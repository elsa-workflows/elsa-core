using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers.Notification;

/// <summary>
/// A workflow definition notifications handler for evicting the cache of the workflow definition service.
/// </summary>
/// <remarks>
/// This service listens for specific notifications and triggers cache invalidation operations accordingly.
/// </remarks>
[UsedImplicitly]
internal class EvictWorkflowDefinitionServiceCache(IWorkflowDefinitionCacheManager workflowDefinitionCacheManager) :
    INotificationHandler<WorkflowDefinitionPublishing>,
    INotificationHandler<WorkflowDefinitionRetracting>,
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsUpdating>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublishing notification, CancellationToken cancellationToken)
    {
        await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionRetracting notification, CancellationToken cancellationToken)
    {
        await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken)
    {
        await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.DefinitionId, cancellationToken);
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
}