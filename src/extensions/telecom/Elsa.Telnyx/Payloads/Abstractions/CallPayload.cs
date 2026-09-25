namespace Elsa.Telnyx.Payloads.Abstractions;

/// <summary>
/// A base class for payloads that are related to a call.
/// </summary>
public abstract record CallPayload : Payload
{
    public string CallControlId { get; init; } = null!;
    public string CallLegId { get; init; } = null!;
    public string CallSessionId { get; init; } = null!;
    public string ConnectionId { get; init; } = null!;
}