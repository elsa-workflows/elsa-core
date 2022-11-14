using System.Text.Json.Serialization;
using Elsa.Telnyx.Converters;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Models
{
    [JsonConverter(typeof(PayloadJsonConverter))]
    public class TelnyxWebhookData : TelnyxRecord
    {
        public string EventType { get; set; } = default!;
        public Guid Id { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public Payload Payload { get; set; } = default!;
    }
}