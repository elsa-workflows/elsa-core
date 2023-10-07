using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Serialization;

/// <summary>
/// Converts json payloads received from Telnyx into concrete <see cref="Payload"/> objects.
/// </summary>
public class WebhookDataJsonConverter : JsonConverter<TelnyxWebhookData>
{
    /// <inheritdoc />
    public override TelnyxWebhookData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dataModel = JsonElement.ParseValue(ref reader);
        var eventType = dataModel.GetProperty("event_type", "EventType").GetString()!;
        var id = dataModel.GetProperty("id", "Id").GetGuid();
        var occurredAt = dataModel.GetProperty("occurred_at", "OccurredAt").GetDateTimeOffset();
        var recordType = dataModel.GetProperty("record_type", "RecordType").GetString()!;
        var payload = PayloadSerializer.Deserialize(eventType, dataModel.GetProperty("payload", "Payload"), options);

        return new TelnyxWebhookData(eventType, id, occurredAt, recordType, payload);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TelnyxWebhookData value, JsonSerializerOptions options)
    {
        var localOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };
            
        var model = JsonSerializer.SerializeToElement(value);
        JsonSerializer.Serialize(writer, model, localOptions);
    }
}