using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;

namespace Elsa.Activities.Telnyx.Webhooks.Services
{
    internal interface IWebhookFilter
    {
        int Priority { get; }
        bool CanHandlePayload(Payload payload);
        string GetActivityTypeName(Payload payload);
    }
}