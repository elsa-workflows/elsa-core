namespace Elsa.Telnyx.Payloads.Abstractions;

public abstract record CallPlayback : CallPayload
{
    public Uri MediaUrl { get; init; } = default!;
    public bool Overlay { get; set; }
}