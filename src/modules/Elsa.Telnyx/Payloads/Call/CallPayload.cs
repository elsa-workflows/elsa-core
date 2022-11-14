using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Payloads.Call
{
    public abstract record CallPayload : Payload
    {
        public string CallControlId { get; init; } = default!;
        public string CallLegId { get; init; } = default!;
        public string CallSessionId { get; init; } = default!;
        public string ConnectionId { get; init; } = default!;
    }
}