using System;
using Elsa.Activities.Telnyx.Converters;
using Elsa.Activities.Telnyx.Payloads.Abstract;
using Newtonsoft.Json;
using NodaTime;

namespace Elsa.Activities.Telnyx.Models
{
    [JsonConverter(typeof(TelnyxWebhookDataJsonConverter))]
    public class TelnyxWebhookData : TelnyxRecord
    {
        public string EventType { get; set; }
        public Guid Id { get; set; }
        public Instant OccurredAt { get; set; }
        public Payload Payload { get; set; }
    }
}