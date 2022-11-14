using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Services
{
    internal interface IWebhookFilter
    {
        int Priority { get; }
        bool CanHandlePayload(Payload payload);
        string GetActivityTypeName(Payload payload);
    }
}