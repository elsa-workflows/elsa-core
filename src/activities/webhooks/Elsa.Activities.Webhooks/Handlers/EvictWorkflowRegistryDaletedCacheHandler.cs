using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Services.Workflows;
using Elsa.Webhooks.Events;
using MediatR;

namespace Elsa.Activities.Webhooks.Handlers
{
    public class EvictWorkflowRegistryDeletedCacheHandler : INotificationHandler<WebhookDefinitionDeleted>
    {
        private readonly ICacheSignal _cacheSignal;

        public EvictWorkflowRegistryDeletedCacheHandler(ICacheSignal cacheSignal)
        {
            _cacheSignal = cacheSignal;
        }

        public async Task Handle(WebhookDefinitionDeleted notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(ActivityTypeService.CacheKey);
        }
    }
}