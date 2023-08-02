using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Models;

namespace Elsa.Telnyx.Events;

/// <summary>
/// Triggered when a Telnyx webhook is received.
/// </summary>
public class TelnyxWebhookReceived : INotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelnyxWebhookReceived"/> class.
    /// </summary>
    public TelnyxWebhookReceived(TelnyxWebhook webhook)
    {
        Webhook = webhook;
    }
        
    /// <summary>
    /// Gets the webhook.
    /// </summary>
    public TelnyxWebhook Webhook { get; }

}