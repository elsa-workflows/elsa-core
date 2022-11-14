using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Services
{
    internal interface IWebhookFilterService
    {
        string? GetActivityTypeName(Payload payload);
    }
}