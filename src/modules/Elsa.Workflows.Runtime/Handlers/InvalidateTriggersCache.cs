using Elsa.Caching;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// 
/// </summary>
public class InvalidateTriggersCache(ICacheManager cacheManager) : INotificationHandler<WorkflowDefinitionsRefreshed>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkflowDefinitionsRefreshed notification, CancellationToken cancellationToken)
    {
        return cacheManager.TriggerTokenAsync(CachingTriggerStore.CacheInvalidationTokenKey, cancellationToken).AsTask();
    }
}