using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;

namespace Elsa.Activities.Telnyx.Webhooks.Services
{
    internal interface IWebhookFilterService
    {
        string? GetActivityTypeName(Payload payload);
    }
}