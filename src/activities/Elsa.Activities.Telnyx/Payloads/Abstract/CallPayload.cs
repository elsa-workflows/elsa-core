namespace Elsa.Activities.Telnyx.Payloads.Abstract
{
    public abstract class CallPayload : Payload
    {
        public string CallControlId { get; set; }
        public string CallLegId { get; set; }
        public string ConnectionId { get; set; }
    }
}