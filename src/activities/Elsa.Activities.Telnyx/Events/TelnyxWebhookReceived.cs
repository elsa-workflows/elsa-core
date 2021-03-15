using Elsa.Activities.Telnyx.Models;
using MediatR;

namespace Elsa.Activities.Telnyx.Events
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