using Elsa.Http.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Http.Handlers;

/// <summary>
/// A handler that invalidates the HTTP workflows cache when a workflow definition is published, retracted, or deleted or when triggers are indexed.
/// </summary>
[UsedImplicitly]
public class InvalidateHttpWorkflowsCache(IHttpWorkflowsCacheManager httpWorkflowsCacheManager) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowTriggersIndexed>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        return InvalidateCacheAsync(notification.WorkflowDefinition.DefinitionId);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    {
        return InvalidateCacheAsync(notification.WorkflowDefinition.DefinitionId);
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        return InvalidateCacheAsync(notification.DefinitionId);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var hashes = new List<string>();
        hashes.AddRange(notification.IndexedWorkflowTriggers.RemovedTriggers.Select(x => x.Hash)!);
        hashes.AddRange(notification.IndexedWorkflowTriggers.AddedTriggers.Select(x => x.Hash)!);

        foreach (string hash in hashes)
            await httpWorkflowsCacheManager.EvictTriggerAsync(hash, cancellationToken);

        await InvalidateCacheAsync(notification.IndexedWorkflowTriggers.Workflow.Identity.DefinitionId);
    }

    private async Task InvalidateCacheAsync(string workflowDefinitionId)
    {
        await httpWorkflowsCacheManager.EvictWorkflowAsync(workflowDefinitionId);
    }
}