using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;

namespace Elsa.Activities.Telnyx.Webhooks.Services
{
    internal class WebhookFilterService : IWebhookFilterService
    {
        private readonly IEnumerable<IWebhookFilter> _filters;

        public WebhookFilterService(IEnumerable<IWebhookFilter> filters)
        {
            _filters = filters.OrderByDescending(x => x.Priority).ToList();
        }
        
        public string? GetActivityTypeName(Payload payload)
        {
            var filter = _filters.FirstOrDefault(x => x.CanHandlePayload(payload));
            return filter?.GetActivityTypeName(payload);
        }
    }
}