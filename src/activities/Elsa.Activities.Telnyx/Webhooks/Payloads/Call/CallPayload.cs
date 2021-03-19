using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    public abstract record CallPayload : Payload, ICorrelationId
    {
        public string CallControlId { get; init; } = default!;
        public string CallLegId { get; init; } = default!;
        public string CallSessionId { get; init; } = default!;
        public string ConnectionId { get; init; } = default!;
        string ICorrelationId.CorrelationId => CallControlId;
    }
}