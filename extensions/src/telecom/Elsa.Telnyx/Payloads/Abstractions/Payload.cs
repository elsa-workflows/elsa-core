namespace Elsa.Telnyx.Payloads.Abstractions;

public abstract record Payload
{
    public string? ClientState { get; set; }
}