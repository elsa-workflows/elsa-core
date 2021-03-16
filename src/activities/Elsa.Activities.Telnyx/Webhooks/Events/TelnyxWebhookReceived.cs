using Elsa.Activities.Telnyx.Webhooks.Models;
using MediatR;

namespace Elsa.Activities.Telnyx.Webhooks.Events
{
    public class TelnyxWebhookReceived : INotification
    {
        public TelnyxWebhookReceived(TelnyxWebhook webhook)
        {
            Webhook = webhook;
        }
        
        public TelnyxWebhook Webhook { get; }

    }
}