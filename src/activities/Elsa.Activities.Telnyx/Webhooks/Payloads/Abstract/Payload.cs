namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract
{
    public abstract record Payload
    {
        public string? ClientState { get; set; }
    }
}