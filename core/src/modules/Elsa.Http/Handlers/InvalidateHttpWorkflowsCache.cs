using Elsa.Http.Bookmarks;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
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
public class InvalidateHttpWorkflowsCache(
    IHttpWorkflowsCacheManager httpWorkflowsCacheManager,
    ITriggerStore triggerStore) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionVersionsUpdated>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>,
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<RequiredWorkflowTriggerRecordsRemoved>,
    INotificationHandler<WorkflowDefinitionsReloaded>
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
        foreach (WorkflowDefinition versionDefinition in notification.WorkflowDefinitions)
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
        foreach (var definitionId in notification.DefinitionIds)
            await InvalidateCacheAsync(definitionId);

        foreach (string id in notification.Ids)
        {
            await InvalidateTriggerCacheForDefinitionVersionAsync(id, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var indexed = notification.IndexedWorkflowTriggers;
        await InvalidateIndexedTriggerHashesAsync(indexed.RemovedTriggers.Concat(indexed.AddedTriggers), cancellationToken);
        await InvalidateCacheAsync(indexed.Workflow.Identity.DefinitionId);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsReloaded notification, CancellationToken cancellationToken)
    {
        foreach (var reloadedWorkflowDefinition in notification.ReloadedWorkflowDefinitions) 
            await InvalidateCacheAsync(reloadedWorkflowDefinition.DefinitionId);
    }

    Task INotificationHandler<RequiredWorkflowTriggerRecordsRemoved>.HandleAsync(RequiredWorkflowTriggerRecordsRemoved notification, CancellationToken cancellationToken)
    {
        return InvalidateIndexedTriggerHashesAsync(notification.Triggers, cancellationToken);
    }

    private async Task InvalidateIndexedTriggerHashesAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken)
    {
        foreach (var hash in triggers.Select(trigger => trigger.Hash).OfType<string>().Distinct())
        {
            await httpWorkflowsCacheManager.EvictTriggerAsync(hash, cancellationToken);
        }
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
        foreach (var trigger in triggers)
        {
            if (trigger.Payload is HttpEndpointBookmarkPayload httpPayload)
            {
                var hash = httpWorkflowsCacheManager.ComputeBookmarkHash(httpPayload.Path, httpPayload.Method);
                await httpWorkflowsCacheManager.EvictTriggerAsync(hash, cancellationToken);
            }
        }
    }
}
