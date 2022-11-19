using Elsa.Mediator.Services;
using Elsa.Telnyx.Models;

namespace Elsa.Telnyx.Events;

public class TelnyxWebhookReceived : INotification
{
    public TelnyxWebhookReceived(TelnyxWebhook webhook)
    {
        Webhook = webhook;
    }
        
    public TelnyxWebhook Webhook { get; }

}