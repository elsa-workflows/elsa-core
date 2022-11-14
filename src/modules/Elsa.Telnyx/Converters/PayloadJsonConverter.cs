using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Converters
{
    /// <summary>
    /// Converts json payloads received from Telnyx into concrete <see cref="Payload"/> objects.
    /// </summary>
    public class PayloadJsonConverter : JsonConverter<Payload>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PayloadJsonConverter()
        {
            var payloadTypes = typeof(PayloadJsonConverter).Assembly.GetTypes().Where(x => typeof(Payload).IsAssignableFrom(x));

            var query =
                from payloadType in payloadTypes
                let payloadAttribute = payloadType.GetCustomAttribute<WebhookAttribute>()
                where payloadAttribute != null
                select (payloadType, payloadAttribute);

            PayloadTypeDictionary = query.ToDictionary(x => x.payloadAttribute!.EventType, x => x.payloadType);
        }
        
        private IDictionary<string, Type> PayloadTypeDictionary { get; }

        /// <inheritdoc />
        public override Payload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dataModel = JsonElement.ParseValue(ref reader);

            var eventType = dataModel.GetProperty("event_type").GetString()!;
            var payloadType = PayloadTypeDictionary.ContainsKey(eventType) ? PayloadTypeDictionary[eventType] : typeof(UnsupportedPayload);
            return (Payload) Activator.CreateInstance(payloadType)!;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Payload value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}