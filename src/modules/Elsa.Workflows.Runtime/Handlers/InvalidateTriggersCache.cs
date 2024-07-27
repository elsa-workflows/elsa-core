using Elsa.Caching;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stores;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// A notification handler that invalidates triggers cache when workflow definitions are refreshed.
/// </summary>
/// <remarks>
/// The class implements the <c>INotificationHandler</c> interface and is responsible for handling <c>WorkflowDefinitionsRefreshed</c> notifications.
/// When a <c>WorkflowDefinitionsRefreshed</c> notification is received, the <c>HandleAsync</c> method is called to invalidate the triggers cache.
/// The cache is invalidated by calling the <c>TriggerTokenAsync</c> method of the <c>ICacheManager</c> passed to the class constructor.
/// </remarks>
[UsedImplicitly]
public class InvalidateTriggersCache(ICacheManager cacheManager) :
    INotificationHandler<WorkflowDefinitionsRefreshed>,
    INotificationHandler<WorkflowDefinitionsReloaded>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsRefreshed notification, CancellationToken cancellationToken)
    {
        return cacheManager.TriggerTokenAsync(CachingTriggerStore.CacheInvalidationTokenKey, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsReloaded notification, CancellationToken cancellationToken)
    {
        await cacheManager.TriggerTokenAsync(CachingTriggerStore.CacheInvalidationTokenKey, cancellationToken);
    }
}