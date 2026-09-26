namespace Elsa.Telnyx.Payloads.Abstractions;

public abstract record CallPlayback : CallPayload
{
    public Uri MediaUrl { get; init; } = null!;
    public bool Overlay { get; set; }
}