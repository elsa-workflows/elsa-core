namespace Elsa.Telnyx.Payloads.Abstractions;

/// <summary>
/// A base class for payloads that are related to a call.
/// </summary>
public abstract record CallPayload : Payload
{
    public string CallControlId { get; init; } = default!;
    public string CallLegId { get; init; } = default!;
    public string CallSessionId { get; init; } = default!;
    public string ConnectionId { get; init; } = default!;
}