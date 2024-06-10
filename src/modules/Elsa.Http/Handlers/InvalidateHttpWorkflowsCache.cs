using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Http.Handlers;

/// <summary>
/// A handler that invalidates the HTTP workflows cache when a workflow definition is published, retracted, or deleted or when triggers are indexed.
/// </summary>
[UsedImplicitly]
public class InvalidateHttpWorkflowsCache(IHttpWorkflowsCacheManager httpWorkflowsCacheManager,
    ITriggerStore triggerStore,
    IHttpWorkflowsCacheManager cacheManager) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionVersionsUpdated>,
    INotificationHandler<WorkflowDefinitionDeleted>, 
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>,
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
    public async Task HandleAsync(WorkflowDefinitionVersionsUpdated notification, CancellationToken cancellationToken)
    {
        foreach (WorkflowDefinitionVersionUpdate versionDefinition in notification.VersionUpdates)
        {
            await InvalidateTriggerCacheForDefinitionVersionAsync(versionDefinition.Id, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        return InvalidateCacheAsync(notification.DefinitionId);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string definitionId in notification.DefinitionIds)
        {
            await InvalidateCacheAsync(definitionId);
        }
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken)
    {
        return InvalidateTriggerCacheForDefinitionVersionAsync(notification.WorkflowDefinition.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.Ids)
        {
            await InvalidateTriggerCacheForDefinitionVersionAsync(id, cancellationToken);
        }
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

    private async Task InvalidateTriggerCacheForDefinitionVersionAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken)
    {
        var filter = new TriggerFilter
        {
            WorkflowDefinitionVersionId = workflowDefinitionVersionId
        };
        var triggers = await triggerStore.FindManyAsync(filter, cancellationToken);
        
        await InvalidateTriggerCacheAsync(triggers, cancellationToken);
    }

    private async Task InvalidateTriggerCacheAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken)
    {
        foreach (StoredTrigger trigger in triggers)
        {
            if (trigger?.Payload is HttpEndpointBookmarkPayload httpPayload)
            {
                var hash = cacheManager.ComputeBookmarkHash(httpPayload.Path, httpPayload.Method);
                await httpWorkflowsCacheManager.EvictTriggerAsync(hash, cancellationToken);
            }
        }
    }
}