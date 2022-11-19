using System.Text.Json;
using Elsa.Telnyx.Payloads;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Helpers;

internal static class PayloadSerializer
{
    public static Payload Deserialize(string eventType, JsonElement dataModel, JsonSerializerOptions? options = null)
    {
        var payloadType = WebhookPayloadTypes.PayloadTypeDictionary.ContainsKey(eventType) ? WebhookPayloadTypes.PayloadTypeDictionary[eventType] : typeof(UnsupportedPayload);
        return (Payload)dataModel.Deserialize(payloadType, options)!;
    }
}