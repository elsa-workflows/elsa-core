using System.Text.Json.Serialization;

namespace Elsa.Webhooks.Models;

/// <summary>
/// Represents a webhook url registration
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
    public WebhookRegistration(Uri url)
    {
        Url = url;
    }
    
    /// <summary>
    /// The URL to deliver the webhook event to.
    /// </summary>
    public Uri Url { get; set; } = default!;

    /// <summary>
    /// A whitelist of event types to deliver. If empty, all events will be delivered.
    /// </summary>
    public HashSet<string> EventTypes { get; set; } = new();
}