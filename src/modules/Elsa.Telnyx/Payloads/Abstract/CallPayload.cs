namespace Elsa.Telnyx.Payloads.Abstract;

public abstract record CallPayload : Payload
{
    public string CallControlId { get; init; } = default!;
    public string CallLegId { get; init; } = default!;
    public string CallSessionId { get; init; } = default!;
    public string ConnectionId { get; init; } = default!;
}