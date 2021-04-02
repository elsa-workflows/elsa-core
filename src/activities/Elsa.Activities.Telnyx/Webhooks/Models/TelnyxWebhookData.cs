using System;
using Elsa.Activities.Telnyx.Webhooks.Converters;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Newtonsoft.Json;
using NodaTime;

namespace Elsa.Activities.Telnyx.Webhooks.Models
{
    [JsonConverter(typeof(TelnyxWebhookDataJsonConverter))]
    public class TelnyxWebhookData : TelnyxRecord
    {
        public string EventType { get; set; } = default!;
        public Guid Id { get; set; }
        public Instant OccurredAt { get; set; }
        public Payload Payload { get; set; } = default!;
    }
}