namespace Elsa.Telnyx.Payloads.Abstract;

public abstract record Payload
{
    public string? ClientState { get; set; }
}