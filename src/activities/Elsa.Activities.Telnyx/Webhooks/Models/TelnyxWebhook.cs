using System;

namespace Elsa.Activities.Telnyx.Webhooks.Models
{
    [Serializable]
    public class TelnyxWebhook
    {
        public TelnyxWebhookMeta Meta { get; set; }
        public TelnyxWebhookData Data { get; set; }
    }
}
