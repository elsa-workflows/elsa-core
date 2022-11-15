using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Models
{
    public record TelnyxWebhookData(string EventType, Guid Id, DateTimeOffset OccurredAt, string RecordType, Payload Payload) : TelnyxRecord(EventType);
}