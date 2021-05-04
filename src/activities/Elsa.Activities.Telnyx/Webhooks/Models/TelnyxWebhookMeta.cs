namespace Elsa.Activities.Telnyx.Webhooks.Models
{
    public class TelnyxWebhookMeta
    {
        public int Attempt { get; set; }
        public string DeliveredTo { get; set; } = default!;
    }
}
