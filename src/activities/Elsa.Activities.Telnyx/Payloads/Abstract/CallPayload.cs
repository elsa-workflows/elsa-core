namespace Elsa.Activities.Telnyx.Payloads.Abstract
{
    public abstract class CallPayload : Payload, ICorrelationId
    {
        public string CallControlId { get; set; }
        public string CallLegId { get; set; }
        public string ConnectionId { get; set; }
        string ICorrelationId.CorrelationId => ConnectionId;
    }
}