using System.Text.Json;
using Elsa.Telnyx.Payloads;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Helpers;

internal static class PayloadSerializer
{
    public static Payload Deserialize(string eventType, JsonElement dataModel, JsonSerializerOptions? options = null)
    {
        var payloadType = WebhookPayloadTypes.PayloadTypeDictionary.TryGetValue(eventType, out var value) ? value : typeof(UnsupportedPayload);
        return (Payload)dataModel.Deserialize(payloadType, options)!;
    }
}