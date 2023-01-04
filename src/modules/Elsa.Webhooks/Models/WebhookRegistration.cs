using System.Text.Json.Serialization;

namespace Elsa.Webhooks.Models;

/// <summary>
/// Represents a webhook endpoint registration
/// </summary>
public class WebhookRegistration
{
    /// <summary>
    /// Constructor.
    /// </summary>
    [JsonConstructor]
    public WebhookRegistration()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public WebhookRegistration(Uri endpoint)
    {
        Endpoint = endpoint;
    }
    
    /// <summary>
    /// The URL to deliver the webhook event to.
    /// </summary>
    public Uri Endpoint { get; set; } = default!;

    /// <summary>
    /// A whitelist of event types to deliver. If empty, all events will be delivered.
    /// </summary>
    public HashSet<string> EventTypes { get; set; } = new();
}