using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Services.Workflows;
using Elsa.Webhooks.Events;
using MediatR;

namespace Elsa.Activities.Webhooks.Handlers
{
    public class EvictWorkflowRegistrySavedCacheHandler : INotificationHandler<WebhookDefinitionSaved>
    {
        private readonly ICacheSignal _cacheSignal;

        public EvictWorkflowRegistrySavedCacheHandler(ICacheSignal cacheSignal)
        {
            _cacheSignal = cacheSignal;
        }

        public async Task Handle(WebhookDefinitionSaved notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(ActivityTypeService.CacheKey);
        }
    }
}