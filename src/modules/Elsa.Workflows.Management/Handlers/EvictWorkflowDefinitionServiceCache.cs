using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// A workflow definition notifications handler for evicting the cache of the workflow definition service.
/// </summary>
/// <remarks>
/// This service listens for specific notifications and triggers cache invalidation operations accordingly.
/// </remarks>
[UsedImplicitly]
internal class EvictWorkflowDefinitionServiceCache(IWorkflowDefinitionCacheManager workflowDefinitionCacheManager) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    {
        await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(notification.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (var definitionId in notification.DefinitionIds)
            await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(definitionId, cancellationToken);
    }
}