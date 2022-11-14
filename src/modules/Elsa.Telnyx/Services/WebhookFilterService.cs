using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Services
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