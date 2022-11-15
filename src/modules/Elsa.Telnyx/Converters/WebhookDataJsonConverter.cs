using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Json.NamingPolicies;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Workflows.Core;

namespace Elsa.Telnyx.Converters
{
    /// <summary>
    /// Converts json payloads received from Telnyx into concrete <see cref="Payload"/> objects.
    /// </summary>
    public class WebhookDataJsonConverter : JsonConverter<TelnyxWebhookData>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public WebhookDataJsonConverter()
        {
            var payloadTypes = WebhookPayloadTypes.PayloadTypes;

            var query =
                from payloadType in payloadTypes
                let payloadAttribute = payloadType.GetCustomAttribute<WebhookAttribute>()
                where payloadAttribute != null
                select (payloadType, payloadAttribute);

            PayloadTypeDictionary = query.ToDictionary(x => x.payloadAttribute!.EventType, x => x.payloadType);
        }
        
        private IDictionary<string, Type> PayloadTypeDictionary { get; }

        /// <inheritdoc />
        public override TelnyxWebhookData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dataModel = JsonElement.ParseValue(ref reader);
            var eventType = dataModel.GetProperty("event_type", "EventType").GetString()!;
            var id = dataModel.GetProperty("id", "Id").GetGuid();
            var occurredAt = dataModel.GetProperty("occurred_at", "OccurredAt").GetDateTimeOffset();
            var recordType = dataModel.GetProperty("record_type", "RecordType").GetString()!;
            var payloadType = PayloadTypeDictionary.ContainsKey(eventType) ? PayloadTypeDictionary[eventType] : typeof(UnsupportedPayload);
            var payload = (Payload)dataModel.GetProperty("payload", "Payload").Deserialize(payloadType, options)!;

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
}