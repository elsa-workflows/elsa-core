using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Services.Workflows;
using Elsa.Webhooks.Events;
using MediatR;

namespace Elsa.Activities.Webhooks.Handlers
{
    public class EvictWorkflowRegistryCacheHandler : INotificationHandler<WebhookDefinitionSaved>, INotificationHandler<WebhookDefinitionDeleted>
    {
        private readonly ICacheSignal _cacheSignal;

        public EvictWorkflowRegistryCacheHandler(ICacheSignal cacheSignal)
        {
            _cacheSignal = cacheSignal;
        }

        public async Task Handle(WebhookDefinitionSaved notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(ActivityTypeService.CacheKey);
        }

        public async Task Handle(WebhookDefinitionDeleted notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(ActivityTypeService.CacheKey);
        }
    }
}