using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// A notification handler that invalidates the workflow cache when workflow definitions are reloaded.
/// </summary>
/// <remarks>
/// The class implements the <c>INotificationHandler</c> interface and is responsible for handling <c>WorkflowDefinitionsReloaded</c> notifications.
/// When a <c>WorkflowDefinitionsReloaded</c> notification is received, the <c>HandleAsync</c> method is called to invalidate the http definition cache.
/// </remarks>
[UsedImplicitly]
public class InvalidateWorkflowsCache(IWorkflowDefinitionCacheManager workflowDefinitionCacheManager) : INotificationHandler<WorkflowDefinitionsReloaded>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsReloaded notification, CancellationToken cancellationToken)
    {
        foreach (var reloadedWorkflowDefinition in notification.ReloadedWorkflowDefinitions)
            await workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(reloadedWorkflowDefinition.DefinitionId, cancellationToken);
    }
}